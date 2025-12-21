using AutoMapper;
using Library.API.Context;
using Library.API.Dtos.BorrowRecordDtos;
using Library.API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Library.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bu controller altındaki tüm aksiyonlar için login zorunlu
    public class BorrowRecordsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public BorrowRecordsController(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // 🔹 1) Tüm ödünç kayıtlarını getir (SADECE ADMIN)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var records = await _context.BorrowRecords
                .Include(x => x.Book)
                .Include(x => x.User)
                .ToListAsync();

            var result = _mapper.Map<List<BorrowRecordListDto>>(records);
            return Ok(result);
        }

        // 🔹 2) Belirli kullanıcının geçmişi (Admin kullanıcıların kitap geçmişini görebilsin diye)
        [HttpGet("user/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetByUserId(int userId)
        {
            var records = await _context.BorrowRecords
                .Include(x => x.Book)
                .Include(x => x.User)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.BorrowDate)
                .ToListAsync();

            var result = _mapper.Map<List<BorrowRecordListDto>>(records);
            return Ok(result);
        }

        // 🔹 3) Giriş yapan kullanıcının AKTİF ödünçleri
        [HttpGet("me/active")]
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> GetMyActive()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            if (!int.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized();

            var records = await _context.BorrowRecords
                .Include(x => x.Book)
                .Include(x => x.User)
                .Where(x => x.UserId == currentUserId && !x.IsReturned)
                .OrderBy(x => x.ReturnDate)
                .ToListAsync();

            var result = _mapper.Map<List<BorrowRecordListDto>>(records);
            return Ok(result);
        }

        // 🔹 4) Giriş yapan kullanıcının TÜM ödünç geçmişi
        [HttpGet("me/history")]
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> GetMyHistory()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            if (!int.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized();

            var records = await _context.BorrowRecords
                .Include(x => x.Book)
                .Include(x => x.User)
                .Where(x => x.UserId == currentUserId)
                .OrderByDescending(x => x.BorrowDate)
                .ToListAsync();

            var result = _mapper.Map<List<BorrowRecordListDto>>(records);
            return Ok(result);
        }

        // 🔹 5) Kitap ödünç alma (UserId TOKEN'dan geliyor)
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> Create([FromBody] BorrowRecordCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            if (!int.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized();

            var book = await _context.Books.FindAsync(dto.BookId);
            var user = await _context.Users.FindAsync(currentUserId);

            if (book == null)
                return BadRequest("Book not found.");

            if (user == null)
                return BadRequest("User not found.");

            if (book.Status == BookStatus.Unavailable)
                return BadRequest("This book is currently borrowed by another user.");

            var borrow = new BorrowRecord
            {
                BookId = dto.BookId,
                UserId = currentUserId,
                BorrowDate = DateTime.Now,
                //ReturnDate = DateTime.Now.AddDays(7),
                ReturnDate = DateTime.Now.AddSeconds(59), // TEST İÇİN SÜREYİ KISALTTIN, istersen geri al
                IsReturned = false,
                ReturnRequestStatus = ReturnRequestStatus.None
            };

            book.Status = BookStatus.Unavailable;

            await _context.BorrowRecords.AddAsync(borrow);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Book successfully borrowed.",
                borrow.Id,
                borrow.BorrowDate,
                borrow.ReturnDate
            });
        }

        // 🔹 6) Kullanıcı: Kitabı iade etmek için İADE İSTEĞİ oluşturur
        //     - Kitap henüz iade edilmez, sadece pending olur
        // POST: /api/BorrowRecords/{id}/return-request
        [HttpPost("{id:int}/return-request")]
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> CreateReturnRequest(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            if (!int.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized();

            var record = await _context.BorrowRecords
                .Include(br => br.Book)
                .FirstOrDefaultAsync(br => br.Id == id);

            if (record == null)
                return NotFound("Borrow record not found.");

            // Normal kullanıcı başkasının kaydı için istekte bulunamasın
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (currentUserRole != "Admin" && record.UserId != currentUserId)
                return Forbid();

            if (record.IsReturned)
                return BadRequest("This book is already returned.");

            if (record.ReturnRequestStatus == ReturnRequestStatus.Pending)
                return BadRequest("There is already a pending return request for this record.");

            record.ReturnRequestStatus = ReturnRequestStatus.Pending;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "İade isteğiniz oluşturuldu. Görevli onayladıktan sonra kitap iade edilmiş olacaktır."
            });
        }

        // 🔹 7) ADMIN: Bekleyen tüm iade isteklerini listele (UI'da 'İade İstekleri' sayfası açarsın)
        // GET: /api/BorrowRecords/return-requests/pending
        [HttpGet("return-requests/pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingReturnRequests()
        {
            var records = await _context.BorrowRecords
                .Include(br => br.Book)
                .Include(br => br.User)
                .Where(br => !br.IsReturned && br.ReturnRequestStatus == ReturnRequestStatus.Pending)
                .OrderBy(br => br.ReturnDate)
                .ToListAsync();

            var result = _mapper.Map<List<BorrowRecordListDto>>(records);
            return Ok(result);
        }

        // 🔹 8) ADMIN: İade isteğini ONAYLAR → kitap gerçekten iade edilir
        // PUT: /api/BorrowRecords/{id}/approve-return
        [HttpPut("{id:int}/approve-return")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveReturn(int id)
        {
            var record = await _context.BorrowRecords
                .Include(br => br.Book)
                .Include(br => br.User)
                .FirstOrDefaultAsync(br => br.Id == id);

            if (record == null)
                return NotFound("Borrow record not found.");

            if (record.IsReturned)
                return BadRequest("This book has already been returned.");

            if (record.ReturnRequestStatus != ReturnRequestStatus.Pending)
                return BadRequest("Bu kayıt için bekleyen bir iade isteği yok.");

            record.IsReturned = true;
            record.ReturnDate = DateTime.Now;
            record.ReturnRequestStatus = ReturnRequestStatus.Approved;

            if (record.Book != null)
            {
                record.Book.Status = BookStatus.Available;
                record.Book.ReturnedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "İade isteği onaylandı. Kitap iade edilmiş olarak işaretlendi."
            });
        }

        // 🔹 9) ADMIN: İade isteğini REDDEDER → kullanıcıya 1 uyarı yazılır
        // PUT: /api/BorrowRecords/{id}/reject-return
        [HttpPut("{id:int}/reject-return")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectReturn(int id)
        {
            var record = await _context.BorrowRecords
                .Include(br => br.User)
                .FirstOrDefaultAsync(br => br.Id == id);

            if (record == null)
                return NotFound("Borrow record not found.");

            if (record.IsReturned)
                return BadRequest("This book has already been returned.");

            if (record.ReturnRequestStatus != ReturnRequestStatus.Pending)
                return BadRequest("Bu kayıt için bekleyen bir iade isteği yok.");

            if (record.User == null)
                return BadRequest("User not loaded for this record.");

            // İade isteği reddedildi
            record.ReturnRequestStatus = ReturnRequestStatus.Rejected;

            // Ortak ceza sistemi: WarningCount / IsLocked / IsDeleted
            ApplyWarning(record.User);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "İade isteği reddedildi. Kullanıcıya 1 uyarı yazıldı."
            });
        }

        // 🔹 10) Kayıt silme (SADECE ADMIN)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var record = await _context.BorrowRecords.FindAsync(id);
            if (record == null)
                return NotFound("Borrow record not found.");

            _context.BorrowRecords.Remove(record);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 🔧 Ortak ceza sistemi (gecikme + iade reddi aynı mantığı kullansın)
        private static void ApplyWarning(User user)
        {
            // Burada User entity'nde olduğunu varsaydığımız alanlar:
            // int WarningCount, bool IsLocked, bool IsDeleted

            user.WarningCount++;

            if (user.WarningCount >= 3)
            {
                user.IsLocked = true;
                user.IsDeleted = true;
            }
            else if (user.WarningCount >= 2)
            {
                user.IsLocked = true;
            }
        }
    }
}
