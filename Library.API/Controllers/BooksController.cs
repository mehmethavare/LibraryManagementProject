using AutoMapper;
using Library.API.Context;
using Library.API.Dtos.BookDtos;
using Library.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public BooksController(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var books = await _context.Books.ToListAsync();
            var result = _mapper.Map<List<BookListDto>>(books);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
                return NotFound("Book not found.");

            var result = _mapper.Map<BookListDto>(book);
            return Ok(result);
        }

        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableBooks()
        {
            var books = await _context.Books
                .Where(x => x.Status == BookStatus.Available)
                .ToListAsync();

            var result = _mapper.Map<List<BookListDto>>(books);
            return Ok(result);
        }

        [HttpGet("borrowed")]
        public async Task<IActionResult> GetBorrowedBooks()
        {
            var books = await _context.Books
                .Where(x => x.Status == BookStatus.Unavailable)
                .ToListAsync();

            var result = _mapper.Map<List<BookListDto>>(books);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BookCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entity = _mapper.Map<Book>(dto);
            entity.Status = BookStatus.Available;
            await _context.Books.AddAsync(entity);
            await _context.SaveChangesAsync();

            var result = _mapper.Map<BookListDto>(entity);
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, result);
        }

        [HttpPut]
        public async Task<IActionResult> Update(int id, [FromBody] BookUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest("Id mismatch.");

            var book = await _context.Books.FindAsync(id);
            if (book == null)
                return NotFound("Book not found.");

            _mapper.Map(dto, book);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
                return NotFound("Book not found.");

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
