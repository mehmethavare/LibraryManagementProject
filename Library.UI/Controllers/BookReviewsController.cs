using Library.UI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
// 🛑 EKLE: PutAsJsonAsync ve PostAsJsonAsync metotları için bu gereklidir
using System.Net.Http.Json;

namespace Library.UI.Controllers
{
    // İsim değişikliği: ReviewsController -> BookReviewsController
    public class BookReviewsController : Controller
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _configuration;

        public BookReviewsController(IHttpClientFactory httpFactory, IConfiguration configuration)
        {
            _httpFactory = httpFactory;
            _configuration = configuration;
        }

        // --- Yardımcı Metod: Token ve Client Hazırlama ---
        private HttpClient? GetClientWithToken()
        {
            var token = HttpContext.Session.GetString("jwt");
            if (string.IsNullOrEmpty(token)) return null;

            var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var baseUrl = _configuration["ApiSettings:BaseUrl"];
            if (string.IsNullOrEmpty(baseUrl)) baseUrl = "https://localhost:7080/api/";
            if (!baseUrl.EndsWith("/")) baseUrl += "/";

            client.BaseAddress = new Uri(baseUrl);
            return client;
        }

        // ============================================================
        // INDEX: Yorumları Listele
        // ============================================================
        public async Task<IActionResult> Index()
        {
            var client = GetClientWithToken();
            if (client == null) return RedirectToAction("Index", "Login");

            var role = HttpContext.Session.GetString("role");
            string endpoint = (role == "admin") ? "BookReviews" : "BookReviews/me";

            try
            {
                var response = await client.GetAsync(endpoint);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    var list = JsonSerializer.Deserialize<List<BookReviewViewModel>>(json,
                         new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    return View(list ?? new List<BookReviewViewModel>());
                }
            }
            catch
            {
                TempData["Error"] = "Sunucu ile bağlantı kurulamadı.";
            }

            return View(new List<BookReviewViewModel>());
        }

        // ============================================================
        // DELETE: Yorum Silme
        // ============================================================
        public async Task<IActionResult> Delete(int id)
        {
            var client = GetClientWithToken();
            if (client == null) return RedirectToAction("Index", "Login");

            var response = await client.DeleteAsync($"BookReviews/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Yorum başarıyla silindi.";
            }
            else
            {
                TempData["Error"] = "Silme işlemi başarısız. Yetkiniz olmayabilir.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // GET Edit: Yorum düzenleme formunu getirir
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = GetClientWithToken();
            if (client == null) return RedirectToAction("Index", "Login");

            var response = await client.GetAsync($"BookReviews/{id}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var model = JsonSerializer.Deserialize<BookReviewUpdateViewModel>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // NOT: API'den gelen modelde UserId kontrolü yapılmadığı sürece
                // kullanıcı başkasının yorum ID'sini URL'ye yazarak düzenleme formuna ulaşabilir.
                // Yetkilendirmeyi API tarafında güçlendirin.

                return View(model);
            }

            TempData["Error"] = "Düzenlenecek yorum bulunamadı veya yetkiniz yok.";
            return RedirectToAction("Index");
        }

        // ============================================================
        // POST Edit: Yorumu günceller
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> Edit(BookReviewUpdateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Lütfen tüm zorunlu alanları doldurunuz.";
                return View(model);
            }

            var client = GetClientWithToken();
            if (client == null) return RedirectToAction("Index", "Login");

            // 🛑 Güncelleme için PUT isteği gönderiliyor
            var response = await client.PutAsJsonAsync($"BookReviews/{model.Id}", model);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Yorumunuz başarıyla güncellendi.";
                return RedirectToAction("Index");
            }

            var errorMsg = await response.Content.ReadAsStringAsync();
            TempData["Error"] = $"Güncelleme başarısız: {errorMsg}";
            return View(model);
        }
    }
}