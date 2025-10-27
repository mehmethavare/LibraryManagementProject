using AutoMapper;
using Library.API.Context;
using Library.API.Dtos.BorrowRecordDtos;
using Library.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BorrowRecordsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public BorrowRecordsController(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var records = await _context.BorrowRecords
                .Include(x => x.Book)
                .Include(x => x.User)
                .ToListAsync();

            var result = _mapper.Map<List<BorrowRecordListDto>>(records);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BorrowRecordCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var book = await _context.Books.FindAsync(dto.BookId);
            var user = await _context.Users.FindAsync(dto.UserId);

            if (book == null || user == null)
                return BadRequest("Invalid BookId or UserId.");

            if (book.Status == BookStatus.Unavailable)
                return BadRequest("This book is currently borrowed by another user.");

            var borrow = new BorrowRecord
            {
                BookId = dto.BookId,
                UserId = dto.UserId,
                BorrowDate = DateTime.Now,
                ReturnDate = DateTime.Now.AddDays(7),
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

        [HttpPut("return")]
        public async Task<IActionResult> ReturnBook(int id)
        {
            var record = await _context.BorrowRecords
                .Include(x => x.Book)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (record == null)
                return NotFound("Borrow record not found.");

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

        [HttpDelete("{id}")]
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
