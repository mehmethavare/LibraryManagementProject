using AutoMapper;
using FluentValidation;
using Library.API.Context;
using Library.API.Dtos.RequestDtos;
using Library.API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Library.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bu controller'daki tüm endpoint'ler login gerektirir
    public class RequestsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IValidator<RequestCreateDto> _createValidator;
        private readonly IValidator<RequestUpdateStatusDto> _updateValidator;

        public RequestsController(
            AppDbContext context,
            IMapper mapper,
            IValidator<RequestCreateDto> createValidator,
            IValidator<RequestUpdateStatusDto> updateValidator)
        {
            _context = context;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        // 👤 Giriş yapan kullanıcının Id'sini JWT'den almak için helper
        private bool TryGetCurrentUserId(out int userId)
        {
            userId = 0;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out userId);
        }

        // 🔹 1) Kullanıcı yeni istek oluşturur
        // POST: /api/Requests
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RequestCreateDto dto)
        {
            // FluentValidation ile validate et
            var validationResult = await _createValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }
                return ValidationProblem(ModelState);
            }

            if (!TryGetCurrentUserId(out var currentUserId))
                return Unauthorized();

            // DTO -> Entity
            var entity = _mapper.Map<UserRequest>(dto);
            entity.UserId = currentUserId;
            entity.Status = RequestStatus.Pending;
            entity.CreatedAt = DateTime.Now;
            entity.UpdatedAt = null;
            entity.ResolvedAt = null;
            entity.AdminResponse = null;

            _context.UserRequests.Add(entity);
            await _context.SaveChangesAsync();

            // Detay DTO'su ile geri dön (isteğe bağlı)
            var created = await _context.UserRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == entity.Id);

            if (created == null)
                return StatusCode(500, "Request created but could not be loaded back.");

            var result = _mapper.Map<RequestDetailDto>(created);

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        // 🔹 2) Giriş yapan kullanıcının kendi isteklerini listelemesi
        // GET: /api/Requests/me
        [HttpGet("me")]
        public async Task<IActionResult> GetMyRequests()
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Unauthorized();

            var requests = await _context.UserRequests
                .Where(r => r.UserId == currentUserId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var result = _mapper.Map<List<RequestListDto>>(requests);
            return Ok(result);
        }

        // 🔹 3) Belirli isteğin detayını görme
        // - Kullanıcı ise sadece kendi isteğini görebilir
        // - Admin ise herkesinkini görebilir
        // GET: /api/Requests/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Unauthorized();

            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var request = await _context.UserRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound("Request not found.");

            // Normal user sadece kendi isteğini görebilir
            if (currentUserRole != "Admin" && request.UserId != currentUserId)
                return Forbid();

            var dto = _mapper.Map<RequestDetailDto>(request);
            return Ok(dto);
        }

        // 🔹 4) Admin: Tüm istekleri listeleme
        // GET: /api/Requests
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll([FromQuery] RequestStatus? status = null)
        {
            var query = _context.UserRequests
                .Include(r => r.User)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(r => r.Status == status.Value);

            var requests = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var result = _mapper.Map<List<RequestListDto>>(requests);
            return Ok(result);
        }

        // 🔹 5) Admin: İstek durumunu ve admin cevabını güncelleme
        // PUT: /api/Requests/{id}/status
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] RequestUpdateStatusDto dto)
        {
            // FluentValidation
            var validationResult = await _updateValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }
                return ValidationProblem(ModelState);
            }

            var request = await _context.UserRequests.FindAsync(id);
            if (request == null)
                return NotFound("Request not found.");

            request.Status = dto.Status;
            request.AdminResponse = dto.AdminResponse;
            request.UpdatedAt = DateTime.Now;

            if (dto.Status == RequestStatus.Resolved || dto.Status == RequestStatus.Rejected)
            {
                request.ResolvedAt = DateTime.Now;
            }
            else if (dto.Status == RequestStatus.Pending)
            {
                // Pending'e alınırsa çözümlenmiş sayılmaz
                request.ResolvedAt = null;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }
        // 🔹 6) Admin Dashboard: Sadece yanıtlanmamış (Pending) son 5 isteği getir
        // GET: /api/Requests/dashboard-pending
        [HttpGet("dashboard-pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetDashboardPending()
        {
            var requests = await _context.UserRequests
                .Include(r => r.User)
                .Where(r => r.Status == RequestStatus.Pending) // Sadece yanıt bekleyenler
                .OrderByDescending(r => r.CreatedAt)
                .Take(5) // Son 5 kayıt
                .ToListAsync();

            var result = _mapper.Map<List<RequestListDto>>(requests);
            return Ok(result);
        }
    }
}
