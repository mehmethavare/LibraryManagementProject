using AutoMapper;
using Library.API.Context;
using Library.API.Dtos.BookReviewDtos;
using Library.API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Library.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // tüm endpoint'ler login gerektirir
    public class BookReviewsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public BookReviewsController(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // 🔹 1) Tüm yorumlar (SADECE ADMIN)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var reviews = await _context.BookReviews
                .Include(x => x.Book)
                .Include(x => x.User)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var result = _mapper.Map<List<BookReviewListDto>>(reviews);
            return Ok(result);
        }

        // 🔹 2) Id'ye göre yorum (Admin + User)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var review = await _context.BookReviews
                .Include(x => x.Book)
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (review == null)
                return NotFound("Review not found.");

            var result = _mapper.Map<BookReviewListDto>(review);
            return Ok(result);
        }

        // 🔹 3) Belirli bir kitaba ait yorumlar (herkes okuyabilir)
        [HttpGet("book/{bookId}")]
        public async Task<IActionResult> GetByBook(int bookId)
        {
            var reviews = await _context.BookReviews
                .Include(x => x.Book)
                .Include(x => x.User)
                .Where(x => x.BookId == bookId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var result = _mapper.Map<List<BookReviewListDto>>(reviews);
            return Ok(result);
        }

        // 🔹 4) Giriş yapan kullanıcının kendi yorumları
        [HttpGet("me")]
        public async Task<IActionResult> GetMyReviews()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized();

            var reviews = await _context.BookReviews
                .Include(x => x.Book)
                .Include(x => x.User)
                .Where(x => x.UserId == currentUserId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var result = _mapper.Map<List<BookReviewListDto>>(reviews);
            return Ok(result);
        }

        // 🔹 5) Yeni yorum + rating ekle
        //    - Sadece ödünç aldığı (geçmişte veya şu an) kitaplara
        //    - Her kitap için 1 adet değerlendirme
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BookReviewCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized();

            // Kitap var mı?
            var bookExists = await _context.Books.AnyAsync(x => x.Id == dto.BookId);
            if (!bookExists)
                return BadRequest("Book not found.");

            if (dto.Rating < 1 || dto.Rating > 5)
                return BadRequest("Rating must be between 1 and 5.");

            // Kullanıcı bu kitabı hiç ödünç almış mı?
            bool hasBorrowed = await _context.BorrowRecords
                .AnyAsync(br => br.UserId == currentUserId && br.BookId == dto.BookId);

            if (!hasBorrowed)
                return BadRequest("Bu kitabı hiç ödünç almadığınız için değerlendirme yapamazsınız.");

            // Daha önce bu kitap için yorum yapmış mı?
            bool alreadyReviewed = await _context.BookReviews
                .AnyAsync(r => r.UserId == currentUserId && r.BookId == dto.BookId);

            if (alreadyReviewed)
                return BadRequest("Bu kitabı zaten değerlendirdiniz.");

            var entity = new BookReview
            {
                UserId = currentUserId,
                BookId = dto.BookId,
                Comment = dto.Comment,
                Rating = dto.Rating,
                CreatedAt = DateTime.Now
            };

            _context.BookReviews.Add(entity);
            await _context.SaveChangesAsync();

            // 🔴 ÖNEMLİ KISIM: Include ile tekrar yükle, sonra map'le
            var created = await _context.BookReviews
                .Include(r => r.Book)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == entity.Id);

            if (created == null)
                return StatusCode(500, "Review created but could not be loaded back.");

            var result = _mapper.Map<BookReviewListDto>(created);

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        // 🔹 6) Yorum güncelleme
        //    - SADECE yorum sahibi değiştirebilir
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] BookReviewUpdateDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized();

            var review = await _context.BookReviews.FindAsync(id);
            if (review == null)
                return NotFound("Review not found.");

            // Normal kullanıcı sadece kendi yorumunu değiştirebilir
            if (review.UserId != currentUserId)
                return Forbid();

            if (dto.Rating < 1 || dto.Rating > 5)
                return BadRequest("Rating must be between 1 and 5.");

            review.Comment = dto.Comment;
            review.Rating = dto.Rating;
            // İstersen UpdatedAt ekleyip burada set edebilirsin

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // 🔹 7) Yorum silme
        //    - Kullanıcı kendi yorumunu silebilir
        //    - Admin herkesinkini silebilir
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized();

            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var review = await _context.BookReviews.FindAsync(id);
            if (review == null)
                return NotFound("Review not found.");

            // Admin → herkesin yorumunu silebilir
            // Normal user → sadece kendi yorumunu silebilir
            if (currentUserRole != "Admin" && review.UserId != currentUserId)
                return Forbid();

            _context.BookReviews.Remove(review);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
