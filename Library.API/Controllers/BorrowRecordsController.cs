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
                ReturnDate = DateTime.Now.AddSeconds(30), // TEST İÇİN SÜREYİ KISALTTIM
                IsReturned = false
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

        // 🔹 6) Kitap iade etme
        //     - Admin her kaydı iade edebilir
        //     - Normal kullanıcı sadece kendi kaydını iade edebilir
        [HttpPut("return/{id}")]
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> ReturnBook(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            if (!int.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized();

            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var record = await _context.BorrowRecords
                .Include(x => x.Book)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (record == null)
                return NotFound("Borrow record not found.");

            // Admin değilse ve kayıt ona ait değilse iade edemesin
            if (currentUserRole != "Admin" && record.UserId != currentUserId)
                return Forbid();

            if (record.IsReturned)
                return BadRequest("This book is already returned.");

            record.IsReturned = true;
            record.ReturnDate = DateTime.Now;

            record.Book!.Status = BookStatus.Available;
            record.Book.ReturnedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Book returned successfully.",
                returnedAt = record.Book.ReturnedAt
            });
        }

        // 🔹 7) Kayıt silme (SADECE ADMIN)
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
    }
}
