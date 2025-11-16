using AutoMapper;
using Library.API.Context;
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
    [Authorize] // Bu controller altındaki tüm aksiyonlar için login zorunlu
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public UsersController(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // 🔹 1) Tüm kullanıcıları listele (SADECE ADMIN)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var users = await _context.Users.ToListAsync();
            var result = _mapper.Map<List<UserListDto>>(users);
            return Ok(result);
        }

        // 🔹 2) Id'ye göre kullanıcı getir (SADECE ADMIN)
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("User not found.");

            var result = _mapper.Map<UserListDto>(user);
            return Ok(result);
        }

        // 🔹 3) Yeni kullanıcı ekle (SADECE ADMIN - rol seçerek)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] UserCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Aynı email'den var mı kontrolü
            var exists = await _context.Users.AnyAsync(u => u.Email == dto.Email);
            if (exists)
                return BadRequest("This email is already in use.");

            var entity = _mapper.Map<User>(dto);
            await _context.Users.AddAsync(entity);
            await _context.SaveChangesAsync();

            var result = _mapper.Map<UserListDto>(entity);
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, result);
        }

        // 🔹 4) Kullanıcı güncelle (SADECE ADMIN)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UserUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest("Id mismatch.");

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("User not found.");

            // Email değişiyorsa, başka bir kullanıcıda var mı kontrol et
            if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailExists = await _context.Users
                    .AnyAsync(u => u.Email == dto.Email && u.Id != id);
                if (emailExists)
                    return BadRequest("This email is already in use.");
            }

            _mapper.Map(dto, user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 🔹 5) Kullanıcı sil (SADECE ADMIN)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("User not found.");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 🔹 6) Giriş yapmış kullanıcının kendi bilgilerini getir
        //     (Admin de çağırırsa kendi profilini görür)
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            var result = _mapper.Map<UserListDto>(user);
            return Ok(result);
        }

        // 🔹 7) Giriş yapmış kullanıcının kendi bilgilerini güncelle
        //     (Normal kullanıcı sadece kendi Name/Surname/Email'ini değiştirebilir)
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UserUpdateDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            // Email değişiyorsa çakışma kontrolü
            if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailExists = await _context.Users
                    .AnyAsync(u => u.Email == dto.Email && u.Id != userId);
                if (emailExists)
                    return BadRequest("This email is already in use.");
            }

            // Kullanıcının kendi rolünü değiştirmesine izin vermiyoruz
            user.Name = dto.Name;
            user.Surname = dto.Surname;
            user.Email = dto.Email;
            user.PhoneNumber = dto.PhoneNumber;

            await _context.SaveChangesAsync();

            return Ok("Your profile has been updated.");
        }
    }
}
