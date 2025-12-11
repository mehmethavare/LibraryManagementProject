using Library.API.Context;
using Library.API.Dtos.AuthDtos;
using Library.API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Library.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // 🔐 Ortak JWT üretim metodu
        private string GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expires = DateTime.UtcNow.AddMinutes(
                double.Parse(jwtSettings["ExpireMinutes"] ?? "60")
            );

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // 🟢 LOGIN
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var normalizedEmail = request.Email.Trim().ToLower();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            // Kullanıcı hiç yoksa → klasik mesaj
            if (user == null)
                return Unauthorized("Email veya şifre hatalı.");

            // ❌ Hesap silinmişse (3. uyarıdan sonra)
            if (user.IsDeleted)
                return Unauthorized("USÜLSÜZ KULLANIMDAN DOLAYI HESABINIZ SİLİNMİŞTİR");

            // 🔒 Hesap kilitliyse (2. uyarıdan sonra)
            if (user.IsLocked)
                return Unauthorized("HESABINIZ KİLİTLENDİ LÜTFEN YETKİLİ İLE GÖRÜŞÜN");

            // Kullanıcı var ama şifre yanlışsa
            if (user.Password != request.Password)
                return Unauthorized("Email veya şifre hatalı.");

            var tokenString = GenerateJwtToken(user);

            return Ok(new
            {
                token = tokenString,
                user = new
                {
                    id = user.Id,
                    name = user.Name,
                    surname = user.Surname,
                    email = user.Email,
                    role = user.Role,
                    warningCount = user.WarningCount,
                    isLocked = user.IsLocked
                }
            });
        }

        // 🟢 REGISTER (KAYIT OL)
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Surname) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Name, Surname, Email ve Password zorunludur.");
            }

            if (request.Password.Length < 6)
                return BadRequest("Şifre en az 6 karakter olmalıdır.");

            var normalizedEmail = request.Email.Trim().ToLower();

            var emailExists = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == normalizedEmail);

            if (emailExists)
                return BadRequest("Bu email ile zaten bir kullanıcı kayıtlı.");

            var user = new User
            {
                Name = request.Name.Trim(),
                Surname = request.Surname.Trim(),
                Email = request.Email.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
                    ? null
                    : request.PhoneNumber.Trim(),
                Password = request.Password,
                Role = "User",
                WarningCount = 0,
                IsLocked = false,
                IsDeleted = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var tokenString = GenerateJwtToken(user);

            return Ok(new
            {
                token = tokenString,
                user = new
                {
                    id = user.Id,
                    name = user.Name,
                    surname = user.Surname,
                    email = user.Email,
                    role = user.Role,
                    warningCount = user.WarningCount,
                    isLocked = user.IsLocked
                }
            });
        }
    }
}
