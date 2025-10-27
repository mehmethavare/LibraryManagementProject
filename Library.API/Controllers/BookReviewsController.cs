using AutoMapper;
using Library.API.Context;
using Library.API.Dtos.BookReviewDtos;
using Library.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookReviewsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public BookReviewsController(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var reviews = await _context.BookReviews
                .Include(x => x.Book)
                .Include(x => x.User)
                .ToListAsync();

            var result = _mapper.Map<List<BookReviewListDto>>(reviews);
            return Ok(result);
        }

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

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BookReviewCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Book ve User var mı kontrolü
            var bookExists = await _context.Books.AnyAsync(x => x.Id == dto.BookId);
            var userExists = await _context.Users.AnyAsync(x => x.Id == dto.UserId);

            if (!bookExists || !userExists)
                return BadRequest("Invalid BookId or UserId.");

            if (dto.Rating < 1 || dto.Rating > 5)
                return BadRequest("Rating must be between 1 and 5.");

            var entity = _mapper.Map<BookReview>(dto);
            await _context.BookReviews.AddAsync(entity);
            await _context.SaveChangesAsync();

            var result = _mapper.Map<BookReviewListDto>(entity);
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, result);
        }

        [HttpPut]
        public async Task<IActionResult> Update(int id, [FromBody] BookReviewCreateDto dto)
        {
            var review = await _context.BookReviews.FindAsync(id);
            if (review == null)
                return NotFound("Review not found.");

            if (dto.Rating < 1 || dto.Rating > 5)
                return BadRequest("Rating must be between 1 and 5.");

            review.Comment = dto.Comment;
            review.Rating = dto.Rating;
            review.BookId = dto.BookId;
            review.UserId = dto.UserId;
            review.CreatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _context.BookReviews.FindAsync(id);
            if (review == null)
                return NotFound("Review not found.");

            _context.BookReviews.Remove(review);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
