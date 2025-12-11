using AutoMapper;
using Library.API.Context;
using Library.API.Dtos.AuthDtos;
using Library.API.Dtos.UserDtos;
using Library.API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Library.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public UsersController(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // 1) Tüm kullanıcıları listele (SADECE ADMIN)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var users = await _context.Users.ToListAsync();
            var result = _mapper.Map<List<UserListDto>>(users);
            return Ok(result);
        }

        // 2) Id'ye göre kullanıcı getir (SADECE ADMIN)
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found.");

            var result = _mapper.Map<UserListDto>(user);
            return Ok(result);
        }

        // 3) Yeni kullanıcı ekle (SADECE ADMIN)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] UserCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var exists = await _context.Users.AnyAsync(u => u.Email == dto.Email);
            if (exists) return BadRequest("This email is already in use.");

            var entity = _mapper.Map<User>(dto);
            await _context.Users.AddAsync(entity);
            await _context.SaveChangesAsync();

            var result = _mapper.Map<UserListDto>(entity);
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, result);
        }

        // 4) Kullanıcı güncelle (SADECE ADMIN)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UserUpdateDto dto)
        {
            if (id != dto.Id) return BadRequest("Id mismatch.");

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found.");

            if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailExists = await _context.Users.AnyAsync(u => u.Email == dto.Email && u.Id != id);
                if (emailExists) return BadRequest("This email is already in use.");
            }

            _mapper.Map(dto, user);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // 5) Kullanıcı sil (SADECE ADMIN) - hard delete
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found.");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // 6) Giriş yapmış kullanıcının kendi bilgilerini getir
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found.");

            var result = _mapper.Map<UserListDto>(user);
            return Ok(result);
        }

        // 7) Giriş yapmış kullanıcının kendi bilgilerini güncelle
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UserUpdateDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found.");

            if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailExists = await _context.Users.AnyAsync(u => u.Email == dto.Email && u.Id != userId);
                if (emailExists) return BadRequest("This email is already in use.");
            }

            user.Name = dto.Name;
            user.Surname = dto.Surname;
            user.Email = dto.Email;
            user.PhoneNumber = dto.PhoneNumber;

            await _context.SaveChangesAsync();
            return Ok("Your profile has been updated.");
        }

        // 8) Şifre Değiştir (MEVCUT KISIM)
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null) return NotFound("User not found.");

            user.Password = dto.NewPassword;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Password changed successfully." });
        }

        // 🔹 9) Giriş yapmış kullanıcının uyarı bilgilerini getir ("Uyarılarım")
        // GET: /api/Users/me/warnings
        [HttpGet("me/warnings")]
        public async Task<IActionResult> GetMyWarnings()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("User not found.");

            var dto = new UserWarningsDto
            {
                WarningCount = user.WarningCount,
                IsLocked = user.IsLocked,
                IsDeleted = user.IsDeleted,
                ShouldLogout = false,
                StatusMessage = null
            };

            // 🔴 3. uyarı → hesap silinmiş sayılıyor
            if (user.IsDeleted && user.WarningCount >= 3)
            {
                dto.ShouldLogout = true;
                dto.StatusMessage = "3. DEFA KİTABI İADE ETMEDİNİZ HESABINIZDAN ÇIKIŞ YAPILIYOR";
            }
            // 🟠 2. uyarı → hesap kilitli
            else if (user.IsLocked && user.WarningCount >= 2)
            {
                dto.ShouldLogout = true;
                dto.StatusMessage = "2. DEFA KİTABI İADE ETMEDİNİZ HESABINIZDAN ÇIKIŞ YAPILIYOR";
            }

            return Ok(dto);
        }

        // 🔹 10) Admin: Kullanıcının blokajını kaldır
        // PUT: /api/Users/{id}/unlock
        [HttpPut("{id:int}/unlock")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UnlockUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("User not found.");

            if (user.IsDeleted)
                return BadRequest("Bu kullanıcı silinmiş. Blokesi kaldırılamaz.");

            if (!user.IsLocked && user.WarningCount < 2)
                return BadRequest("Bu kullanıcının aktif bir blokesi bulunmuyor.");

            user.IsLocked = false;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Kullanıcının blokesi kaldırıldı.",
                warningCount = user.WarningCount
            });
        }
    }
}
