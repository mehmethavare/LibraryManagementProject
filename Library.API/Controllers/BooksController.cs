using AutoMapper;
using Library.API.Context;
using Library.API.Dtos.BookDtos;
using Library.API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bu controller altındaki tüm endpoint'ler için login zorunlu
    public class BooksController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public BooksController(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // 🔹 1) Tüm kitapları listele (Admin + Normal kullanıcı)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var books = await _context.Books.ToListAsync();
            var result = _mapper.Map<List<BookListDto>>(books);
            return Ok(result);
        }

        // 🔹 2) Id'ye göre kitap getir (Admin + Normal kullanıcı)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
                return NotFound("Book not found.");

            var result = _mapper.Map<BookListDto>(book);
            return Ok(result);
        }

        // 🔹 3) Müsait (Available) kitaplar (Admin + Normal kullanıcı)
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableBooks()
        {
            var books = await _context.Books
                .Where(x => x.Status == BookStatus.Available)
                .ToListAsync();

            var result = _mapper.Map<List<BookListDto>>(books);
            return Ok(result);
        }

        // 🔹 4) Şu an ödünçte olan (Unavailable) kitaplar (Admin + Normal kullanıcı)
        [HttpGet("borrowed")]
        public async Task<IActionResult> GetBorrowedBooks()
        {
            var books = await _context.Books
                .Where(x => x.Status == BookStatus.Unavailable)
                .ToListAsync();

            var result = _mapper.Map<List<BookListDto>>(books);
            return Ok(result);
        }

        // 🔹 5) Yeni kitap ekle (SADECE ADMIN)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] BookCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entity = _mapper.Map<Book>(dto);
            entity.Status = BookStatus.Available; // yeni kitap her zaman müsait başlasın

            await _context.Books.AddAsync(entity);
            await _context.SaveChangesAsync();

            var result = _mapper.Map<BookListDto>(entity);
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, result);
        }

        // 🔹 6) Kitap güncelle (SADECE ADMIN)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
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

        // 🔹 7) Kitap sil (SADECE ADMIN)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
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
