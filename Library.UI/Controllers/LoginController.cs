using Library.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

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
            _baseUrl = config["ApiBaseUrl"] ?? "https://localhost:7080/api/";
        }

        // --- YARDIMCI METOT: FOTOĞRAF URL'SİNİ ÇEKME (API'DEN) ---
        private async Task<string?> GetProfileImageUrl(string token)
        {
            var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var url = _baseUrl.EndsWith("/") ? _baseUrl : _baseUrl + "/";
            var requestUrl = url + "Users/me";

            try
            {
                var response = await client.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var profileResult = JsonDocument.Parse(json).RootElement;

                    // KRİTİK DEĞİŞİKLİK BURADA: profileImageUrl yerine profileImageId'yi çekiyoruz
                    if (profileResult.TryGetProperty("profileImageId", out var imageIdElement) && imageIdElement.ValueKind == JsonValueKind.String)
                    {
                        var imageId = imageIdElement.GetString();

                        // ID boş değilse, URL'yi API'nin dosya servis endpoint'i ile oluşturun
                        if (!string.IsNullOrEmpty(imageId))
                        {
                            // API'nizin dosya sunumu yaptığı adrese göre URL oluşturulur. 
                            // Varsayım: Dosyaları APIBaseUrl + "Files/{ID}" adresinden sunuyorsunuz.
                            return $"{url}Files/{imageId}";
                        }
                    }
                }
            }
            catch { /* Hata durumunda null döndürür */ }
            return null;
        }
        // -------------------------------------------------------------------

        [HttpGet]
        public IActionResult Index()
        {
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
            var response = await http.PostAsync(_baseUrl + "Auth/login", content);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Email veya şifre hatalı.");
                return View(model);
            }

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<LoginResponse>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result != null && result.User != null)
            {
                var token = result.Token;

                // Session kayıtları
                HttpContext.Session.SetInt32("userId", result.User.Id);
                HttpContext.Session.SetString("jwt", token);
                HttpContext.Session.SetString("username", result.User.Email);
                HttpContext.Session.SetString("role", result.User.Role.ToLower());

                // KRİTİK ÇÖZÜM: API'den en güncel profil fotoğrafı URL'sini çek ve Session'a kaydet
                var profileImageUrl = await GetProfileImageUrl(token);

                // Bu Session kaydı, Layout ve Profil sayfalarının fotoğrafı giriş anında görmesini sağlar.
                HttpContext.Session.SetString("profileImageUrl", profileImageUrl ?? "");

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

        // API'den gelen cevap modelleri (ProfileImageUrl, LoginUser'dan çıkarıldı)
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
            // ProfileImageUrl buradan kaldırıldı, çünkü ayrı bir GET ile çekiyoruz.
        }
    }
}