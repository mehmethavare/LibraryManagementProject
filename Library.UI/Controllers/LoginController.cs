using Library.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // Session işlemleri için
using System.Text;
using System.Text.Json;

namespace Library.UI.Controllers
{
    [AllowAnonymous]
    public class LoginController : Controller
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly string _baseUrl;

        public LoginController(IHttpClientFactory httpFactory, IConfiguration config)
        {
            _httpFactory = httpFactory;
            // API adresi appsettings'den gelmezse varsayılanı kullan
            _baseUrl = config["ApiBaseUrl"] ?? "https://localhost:7184/api/";
        }

        [HttpGet]
        public IActionResult Index()
        {
            // Zaten giriş yapmışsa direkt ana sayfaya at
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("jwt")))
            {
                return RedirectToAction("Index", "Home");
            }
            return View(new LoginRequestViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Index(LoginRequestViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var http = _httpFactory.CreateClient();
            var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");

            // API'ye login isteği atıyoruz
            var response = await http.PostAsync(_baseUrl + "Auth/login", content);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Email veya şifre hatalı.");
                return View(model);
            }

            var body = await response.Content.ReadAsStringAsync();

            // Gelen cevabı modele çeviriyoruz
            var result = JsonSerializer.Deserialize<LoginResponse>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result != null && result.User != null)
            {
                // ============================================================
                // BU KISIM ÇOK ÖNEMLİ (Profil ve Ödünç Alma İçin)
                // ============================================================

                // 1. Kullanıcı ID'sini kaydediyoruz (Profil sayfası bunu arıyor)
                HttpContext.Session.SetInt32("userId", result.User.Id);

                // 2. Token ve Kullanıcı Adını kaydediyoruz
                HttpContext.Session.SetString("jwt", result.Token);
                HttpContext.Session.SetString("username", result.User.Email);

                // 3. Rolü küçük harfle kaydediyoruz (admin/user kontrolü için)
                HttpContext.Session.SetString("role", result.User.Role.ToLower());

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Sunucudan hatalı veri döndü.");
            return View(model);
        }

        [AllowAnonymous]
        public IActionResult Logout()
        {
            // Çıkış yapınca her şeyi sil
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Login");
        }

        // API'den gelen cevap modelleri
        private class LoginResponse
        {
            public string Token { get; set; } = string.Empty;
            public LoginUser User { get; set; } = new();
        }

        private class LoginUser
        {
            public int Id { get; set; }
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = "User";
        }
    }
}