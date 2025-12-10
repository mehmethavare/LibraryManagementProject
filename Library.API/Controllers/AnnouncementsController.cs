using AutoMapper;
using FluentValidation;
using Library.API.Context;
using Library.API.Dtos.AnnouncementDtos;
using Library.API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Library.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // duyuruları görmek için login zorunlu; istersen AllowAnonymous yapabiliriz
    public class AnnouncementsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IValidator<AnnouncementCreateDto> _createValidator;
        private readonly IValidator<AnnouncementUpdateDto> _updateValidator;

        public AnnouncementsController(
            AppDbContext context,
            IMapper mapper,
            IValidator<AnnouncementCreateDto> createValidator,
            IValidator<AnnouncementUpdateDto> updateValidator)
        {
            _context = context;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            userId = 0;
            var idValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idValue, out userId);
        }

        // 🔹 Tüm duyuruları listele (kullanıcı + admin)
        // GET: /api/Announcements
        [HttpGet]
        [AllowAnonymous] // istersen bunu kaldırıp sadece login olanlara açabilirsin
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.Announcements
                .Include(a => a.CreatedByUser)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            var result = _mapper.Map<List<AnnouncementListDto>>(list);
            return Ok(result);
        }

        // 🔹 Tek bir duyuru getir
        // GET: /api/Announcements/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var entity = await _context.Announcements
                .Include(a => a.CreatedByUser)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (entity == null)
                return NotFound("Announcement not found.");

            var dto = _mapper.Map<AnnouncementListDto>(entity);
            return Ok(dto);
        }

        // 🔹 Admin: duyuru oluştur
        // POST: /api/Announcements
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] AnnouncementCreateDto dto)
        {
            var validation = await _createValidator.ValidateAsync(dto);
            if (!validation.IsValid)
            {
                foreach (var error in validation.Errors)
                    ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

                return ValidationProblem(ModelState);
            }

            if (!TryGetCurrentUserId(out var currentUserId))
                return Unauthorized();

            var entity = _mapper.Map<Announcement>(dto);
            entity.CreatedAt = DateTime.Now;
            entity.UpdatedAt = null;
            entity.CreatedByUserId = currentUserId;

            _context.Announcements.Add(entity);
            await _context.SaveChangesAsync();

            // tekrar yükleyelim ki CreatedByName dolu gelsin
            var created = await _context.Announcements
                .Include(a => a.CreatedByUser)
                .FirstOrDefaultAsync(a => a.Id == entity.Id);

            var result = _mapper.Map<AnnouncementListDto>(created!);

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        // 🔹 Admin: duyuruyu güncelle
        // PUT: /api/Announcements/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] AnnouncementUpdateDto dto)
        {
            var validation = await _updateValidator.ValidateAsync(dto);
            if (!validation.IsValid)
            {
                foreach (var error in validation.Errors)
                    ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

                return ValidationProblem(ModelState);
            }

            var entity = await _context.Announcements.FindAsync(id);
            if (entity == null)
                return NotFound("Announcement not found.");

            entity.Title = dto.Title;
            entity.Content = dto.Content;
            entity.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // 🔹 Admin: duyuruyu sil
        // DELETE: /api/Announcements/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.Announcements.FindAsync(id);
            if (entity == null)
                return NotFound("Announcement not found.");

            _context.Announcements.Remove(entity);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
