using Library.UI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Library.UI.Controllers
{
    public class BooksController : Controller
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _configuration;

        public BooksController(IHttpClientFactory httpFactory, IConfiguration configuration)
        {
            _httpFactory = httpFactory;
            _configuration = configuration;
        }

        // --- YARDIMCI METODLAR ---
        private string? GetToken() => HttpContext.Session.GetString("jwt");

        private HttpClient GetClient()
        {
            var client = _httpFactory.CreateClient();
            var token = GetToken();

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var baseUrl = _configuration["ApiSettings:BaseUrl"];
            if (string.IsNullOrEmpty(baseUrl)) baseUrl = "https://localhost:7080/api/";
            if (!baseUrl.EndsWith("/")) baseUrl += "/";

            client.BaseAddress = new Uri(baseUrl);
            return client;
        }

        // Bu metod null dönebilir, kullanan metodlar KONTROL ETMELİDİR.
        private HttpClient? GetClientWithStrictToken()
        {
            var token = GetToken();
            if (string.IsNullOrEmpty(token)) return null;
            return GetClient();
        }

        // GET: Index
        public async Task<IActionResult> Index()
        {
            var client = GetClient();
            try
            {
                var res = await client.GetAsync("Books");
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    var list = JsonSerializer.Deserialize<List<BookListViewModel>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return View(list ?? new List<BookListViewModel>());
                }
            }
            catch { /* Loglama yapılabilir */ }

            return View(new List<BookListViewModel>());
        }

        // GET: Details
        public async Task<IActionResult> Details(int id)
        {
            var client = GetClient(); // Token zorunlu değil

            // 1. ADIM: Kitap Bilgisini Çek
            var bookResponse = await client.GetAsync($"Books/{id}");

            if (!bookResponse.IsSuccessStatusCode) return NotFound();

            var bookJson = await bookResponse.Content.ReadAsStringAsync();
            var bookData = JsonSerializer.Deserialize<BookUpdateViewModel>(bookJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (bookData == null) return NotFound();

            // 2. ADIM: O Kitaba Ait Yorumları Çek
            // Düzeltilen endpoint: BookReviews/book/{id}
            var reviewsResponse = await client.GetAsync($"BookReviews/book/{id}");

            List<BookReviewViewModel> reviewsList = new List<BookReviewViewModel>();

            if (reviewsResponse.IsSuccessStatusCode)
            {
                var reviewsJson = await reviewsResponse.Content.ReadAsStringAsync();
                reviewsList = JsonSerializer.Deserialize<List<BookReviewViewModel>>(reviewsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new List<BookReviewViewModel>();
            }

            // 3. ADIM: Modeli Birleştir
            var detailModel = new BookDetailViewModel
            {
                Id = bookData.Id,
                Title = bookData.Title,
                AuthorName = bookData.AuthorName,
                CategoryName = bookData.CategoryName,
                PublishYear = bookData.PublishYear,
                CoverImageUrl = bookData.CoverImageUrl,
                Status = bookData.Status,

                // Yorumları View Model'e dönüştür
                Reviews = reviewsList.Select(r => new ReviewViewModel
                {
                    Id = r.Id,
                    UserName = r.UserName,
                    UserId = r.UserId,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    Date = r.CreatedAt
                }).ToList(),

                ReviewCount = reviewsList.Count,
                AverageRating = reviewsList.Any() ? reviewsList.Average(r => r.Rating) : 0
            };

            return View(detailModel);
        }

        [HttpPost]
        public async Task<IActionResult> AddReview(BookDetailViewModel model)
        {
            var client = GetClientWithStrictToken();
            if (client == null) return RedirectToAction("Index", "Login");

            var reviewDto = new
            {
                BookId = model.Id,
                Rating = model.NewRating,
                Comment = model.NewComment
            };

            // DÜZELTME BURADA: "Reviews" -> "BookReviews"
            var response = await client.PostAsJsonAsync("BookReviews", reviewDto);

            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Yorumunuz eklendi.";
            else
            {
                // Hata mesajını API'den okuyalım (Örn: "Bu kitabı hiç ödünç almadınız")
                var msg = await response.Content.ReadAsStringAsync();
                TempData["Error"] = "Hata: " + msg;
            }

            return RedirectToAction("Details", new { id = model.Id });
        }

        // ADMIN İŞLEMLERİ

        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("role") != "admin") return RedirectToAction("Index", "Login");
            return View(new BookCreateViewModel());
        }

		[HttpPost]
		public async Task<IActionResult> Create(BookCreateViewModel model)
		{
			if (HttpContext.Session.GetString("role") != "admin") return RedirectToAction("Index", "Login");

			// ModelState.IsValid kontrolü API'ye veri göndermeden önce çalışır (View Model'deki [Required] kontrolü).
			if (!ModelState.IsValid)
				return View(model);

			var client = GetClientWithStrictToken();
			if (client == null) return RedirectToAction("Index", "Login");

			// 1. KRİTİK DÜZELTME: Veri Eşleştirme (Mapping)
			// View Model'deki Türkçe alan adlarını API'nin beklediği İngilizce/standart alan adlarına çeviriyoruz.
			var apiModel = new
			{
				Title = model.KitapAdi,
				Authorname = model.YazarAdi,
				CategoryName = model.Kategori,
				PublishYear = model.YayinYili,
                PublisherName = model.YayinciAdi,
                // Eğer View Model'inizde CoverImageUrl varsa, onu da buraya eklemelisiniz.
            };

			// 2. API'ye POST isteği gönder
			var response = await client.PostAsJsonAsync("Books", apiModel); // 👈 Yeni API Modelini gönderiyoruz.

			if (response.IsSuccessStatusCode)
			{
				TempData["SuccessMessage"] = "✅ Yeni kitap başarıyla eklendi.";
				return RedirectToAction(nameof(Index));
			}
			else
			{
				// 3. KRİTİK DÜZELTME: Detaylı Hata Yönetimi
				// API'den gelen hata detayını okuyup kullanıcıya gösteriyoruz.
				var errorContent = await response.Content.ReadAsStringAsync();

				// Bu, API'deki bir zorunlu alanın eksik olması, veri tipi hatası vb. olabilir.
				ModelState.AddModelError("", $"Kayıt başarısız. Durum Kodu: {(int)response.StatusCode}. API Mesajı: {errorContent}");

				return View(model);
			}
		}

		public async Task<IActionResult> Edit(int id)
        {
            if (HttpContext.Session.GetString("role") != "admin") return RedirectToAction("Index", "Login");

            var client = GetClientWithStrictToken();
            if (client == null) return RedirectToAction("Index", "Login"); // NULL KONTROLÜ EKLENDİ

            var response = await client.GetAsync($"Books/{id}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var model = JsonSerializer.Deserialize<BookUpdateViewModel>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return View(model);
            }
            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(BookUpdateViewModel model)
        {
            if (HttpContext.Session.GetString("role") != "admin") return RedirectToAction("Index", "Login");
            if (!ModelState.IsValid) return View(model);

            var client = GetClientWithStrictToken();
            if (client == null) return RedirectToAction("Index", "Login"); // NULL KONTROLÜ EKLENDİ

            var response = await client.PutAsJsonAsync($"Books/{model.Id}", model);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Güncellendi.";
                return RedirectToAction("Edit", new { id = model.Id });
            }

            TempData["Error"] = "Güncelleme başarısız.";
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (HttpContext.Session.GetString("role") != "admin") return RedirectToAction("Index", "Login");

            var client = GetClientWithStrictToken();
            if (client == null) return RedirectToAction("Index", "Login"); // NULL KONTROLÜ EKLENDİ

            await client.DeleteAsync($"Books/{id}");

            TempData["Success"] = "Silindi.";
            return RedirectToAction(nameof(Index));
        }
    }
}