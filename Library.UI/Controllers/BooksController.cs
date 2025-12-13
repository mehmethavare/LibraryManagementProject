using Library.UI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
// using static System.Runtime.InteropServices.JavaScript.JSType; // Bu using gereksiz, kaldırıldı.

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

        // BooksController.cs dosyasındaki Index metodu:
        // BooksController.cs dosyasındaki Index metodu:

        // GET: Index
        public async Task<IActionResult> Index(string? category) // Filtreleme parametresini kabul eder
        {
            var client = GetClient();
            var model = new BookIndexViewModel();
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // 1. ADIM: Kategori Listesini API'den Çek (Değişiklik yok)
            try
            {
                // API Endpoint: /api/Books/categories (Görselde görüldüğü gibi)
                var categoryRes = await client.GetAsync("Books/categories");
                if (categoryRes.IsSuccessStatusCode)
                {
                    var categoryJson = await categoryRes.Content.ReadAsStringAsync();
                    model.Categories = JsonSerializer.Deserialize<List<CategoryViewModel>>(categoryJson, jsonOptions)
                        ?? new List<CategoryViewModel>();
                }
            }
            catch { /* Hata olsa bile devam eder */ }

            // 2. ADIM: Kitap Listesini Çek (KRİTİK GÜNCELLEME BURADA)
            try
            {
                string requestUrl;

                // Filtreleme yapılıp yapılmadığını kontrol et
                if (!string.IsNullOrEmpty(category) && category != "Tümü")
                {
                    // KRİTİK DEĞİŞİKLİK: API'nin beklediği tam endpoint'i kullanıyoruz.
                    // Endpoint: /api/Books/by-category/available?category=roman
                    requestUrl = $"Books/by-category/available?category={Uri.EscapeDataString(category)}";
                    model.SelectedCategory = category;
                }
                else
                {
                    // Tümü seçiliyse, müsait olan tüm kitapları listelemek için özel endpoint'i kullanıyoruz.
                    // Endpoint: /api/Books/available
                    requestUrl = "Books/available";
                    model.SelectedCategory = "Tümü";
                }

                var res = await client.GetAsync(requestUrl);

                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    model.Books = JsonSerializer.Deserialize<List<BookListViewModel>>(json, jsonOptions)
                        ?? new List<BookListViewModel>();
                }
                else
                {
                    // Hata durumunda (örneğin 401 Unauthorized), boş liste döndür.
                    model.Books = new List<BookListViewModel>();
                }
            }
            catch
            {
                model.Books = new List<BookListViewModel>();
            }

            return View(model);
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

            var response = await client.PostAsJsonAsync("BookReviews", reviewDto);

            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Yorumunuz eklendi.";
            else
            {
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

            if (!ModelState.IsValid)
                return View(model);

            var client = GetClientWithStrictToken();
            if (client == null) return RedirectToAction("Index", "Login");

            var apiModel = new
            {
                Title = model.KitapAdi,
                Authorname = model.YazarAdi,
                CategoryName = model.Kategori,
                PublishYear = model.YayinYili,
                PublisherName = model.YayinciAdi,
            };

            var response = await client.PostAsJsonAsync("Books", apiModel);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "✅ Yeni kitap başarıyla eklendi.";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Kayıt başarısız. Durum Kodu: {(int)response.StatusCode}. API Mesajı: {errorContent}");
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            if (HttpContext.Session.GetString("role") != "admin") return RedirectToAction("Index", "Login");

            var client = GetClientWithStrictToken();
            if (client == null) return RedirectToAction("Index", "Login");

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
            if (client == null) return RedirectToAction("Index", "Login");

            var response = await client.PutAsJsonAsync($"Books/{model.Id}", model);

            if (response.IsSuccessStatusCode)
            {
                // ******* KRİTİK DÜZELTME BURADA *******
                // Başarılı güncelleme sonrası Kitap Listesi (Index) sayfasına yönlendir
                TempData["Success"] = "Kitap başarıyla güncellendi.";
                return RedirectToAction(nameof(Index));
            }

            // Hata durumunda hata mesajını al ve aynı View'ı geri döndür
            var errorContent = await response.Content.ReadAsStringAsync();
            TempData["Error"] = $"Güncelleme başarısız. Detay: {response.StatusCode} - {errorContent}";
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (HttpContext.Session.GetString("role") != "admin") return RedirectToAction("Index", "Login");

            var client = GetClientWithStrictToken();
            if (client == null) return RedirectToAction("Index", "Login");

            await client.DeleteAsync($"Books/{id}");

            TempData["Success"] = "Silindi.";
            return RedirectToAction(nameof(Index));
        }
    }
}