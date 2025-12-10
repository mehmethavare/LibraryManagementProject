using Library.UI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Library.UI.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _baseUrl;

        public ProfileController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            // API adresini alıyoruz
            _baseUrl = configuration["ApiBaseUrl"] ?? "https://localhost:7080/api/";
        }

        // ==========================================
        // 1. PROFİL GÖRÜNTÜLEME (READ-ONLY)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = await GetUserProfile();

            if (model == null)
            {
                if (string.IsNullOrEmpty(HttpContext.Session.GetString("jwt")))
                    return RedirectToAction("Index", "Login");

                TempData["Error"] = "Profil bilgileri sunucudan çekilemedi.";
                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        // ==========================================
        // 2. DÜZENLEME SAYFASI (FORM)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var model = await GetUserProfile();

            if (model == null)
            {
                return RedirectToAction("Index", "Login");
            }

            return View(model);
        }

        // ==========================================
        // 3. GÜNCELLEME İŞLEMİ (POST)
        // ==========================================
        // ==========================================
        // 3. GÜNCELLEME İŞLEMİ (POST) - DÜZELTİLMİŞ HALİ
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> Edit(UserUpdateViewModel model)
        {
            var token = HttpContext.Session.GetString("jwt");
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Index", "Login");

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // DÜZELTME BURADA:
            // Hata veren eski kod: _baseUrl + "Users"
            // Yeni kod: "Users/me" (GET metodunda olduğu gibi sonuna 'me' ekliyoruz)

            var url = _baseUrl.EndsWith("/") ? _baseUrl : _baseUrl + "/";
            url += "Users/me";

            try
            {
                // Artık istek "https://localhost:7080/api/Users/me" adresine gidecek
                var response = await client.PutAsJsonAsync(url, model);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Profiliniz başarıyla güncellendi.";
                    return RedirectToAction("Index");
                }
                else
                {
                    // API'den dönen hatayı okuyalım ki 400/500 olursa sebebini görelim
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    ViewBag.ErrorMessage = "Güncelleme başarısız: " + errorMsg; // Hata detayını ekrana basar
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Sunucu ile iletişim hatası: " + ex.Message;
                return View(model);
            }
        }

        // ==========================================
        // YARDIMCI METOT: KULLANICIYI ÇEKME
        // ==========================================
        private async Task<UserUpdateViewModel?> GetUserProfile()
        {
            // Sadece Token kontrolü yeterli, ID'ye URL'de ihtiyacımız yok
            var token = HttpContext.Session.GetString("jwt");
            if (string.IsNullOrEmpty(token)) return null;

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Base URL sonuna / ekleme kontrolü
            var url = _baseUrl.EndsWith("/") ? _baseUrl : _baseUrl + "/";

            // DEĞİŞİKLİK BURADA: Artık ID yerine "me" kullanıyoruz
            url += "Users/me";

            try
            {
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var userProfile = JsonSerializer.Deserialize<UserUpdateViewModel>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    return userProfile;
                }
            }
            catch (Exception)
            {
                // Loglama yapılabilir
            }

            return null;
        }
    }
}