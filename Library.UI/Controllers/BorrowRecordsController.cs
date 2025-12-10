using Library.UI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // Session için
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace Library.UI.Controllers
{
    // Controller'ın BaseController'dan türediğinden emin olun
    public class BorrowRecordsController : BaseController
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly string _baseUrl;

        public BorrowRecordsController(IHttpClientFactory httpFactory, IConfiguration config)
        {
            _httpFactory = httpFactory;
            // Config'in null olması veya API adresinin bulunmaması durumuna karşı kontrol
            _baseUrl = config["ApiBaseUrl"] ?? "http://localhost:5000/api/";
        }

        // ============================================================
        // INDEX (Listeleme) - 401 Hatası Kontrolü (Token Ekleme)
   
        // BorrowRecordsController.cs içinde, Index metodu

   
        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("role");
            var userId = HttpContext.Session.GetInt32("userId");

            var http = _httpFactory.CreateClient();
            AddJwt(http);

            string apiEndpoint;

            // 1. Role göre API uç noktasını belirle
            if (role == "admin")
            {
                // Admin: Tüm kayıtları çeken endpoint (API'deki GetAll metodu)
                apiEndpoint = _baseUrl + "BorrowRecords";
            }
            else if (role == "user" && userId.HasValue)
            {
                // User: Sadece kendi geçmişini çeken endpoint (API'deki GetMyHistory metodu)
                // Bu endpoint'e API'de yetki verildiği için 403 hatası çözülür.
                apiEndpoint = _baseUrl + "BorrowRecords/me/history";
            }
            else
            {
                // Kullanıcı bilgisi eksikse boş liste döndür ve hata mesajı göster
                TempData["ErrorMessage"] = "Ödünç listesi görüntülenemedi. Oturum bilgileri eksik.";
                return View(new List<BorrowRecordListViewModel>());
            }

            // 2. API'ye istek gönder
            var response = await http.GetAsync(apiEndpoint);

            // 3. Başarısızlık durumunu kontrol et
            if (!response.IsSuccessStatusCode)
            {
                // Hata durumunda (403, 500 vb.) boş liste döndür
                TempData["ErrorMessage"] = $"Ödünç listesi yüklenemedi. Durum Kodu: {(int)response.StatusCode}. Lütfen tekrar giriş yapın.";
                return View(new List<BorrowRecordListViewModel>());
            }

            // 4. JSON verisini işle
            var json = await response.Content.ReadAsStringAsync();

            try
            {
                var list = JsonSerializer.Deserialize<List<BorrowRecordListViewModel>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // API'den gelen liste filtrelenmiş olduğu için UI'da ekstra filtrelemeye gerek yoktur.
                return View(list ?? new List<BorrowRecordListViewModel>());
            }
            catch (JsonException)
            {
                // JSON okuma hatası
                TempData["ErrorMessage"] = "API'den gelen veri formatı hatalı.";
                return View(new List<BorrowRecordListViewModel>());
            }
        }
        // ============================================================
        // CREATE (GET) - Onay Sayfası
        // ============================================================
        public IActionResult Create(int? bookId)
        {
            if (HttpContext.Session.GetString("role") == "admin") return RedirectToAction("Index", "Books");

            return View(new BorrowRecordCreateViewModel
            {
                BookId = bookId ?? 0,
                BorrowDate = DateTime.Now
            });
        }

        // ============================================================
        // CREATE (POST) - Ödünç Alma İşlemi (400 Hatası Çözümü)
        // ============================================================
        // ============================================================
        // CREATE (POST) - Ödünç Alma İşlemi
        // Bu metot, Books/index.cshtml dosyasındaki formdan gelen POST isteğini karşılar.
        // ============================================================
        [HttpPost]
        // İsteği yakalamak için model yerine BookId parametresini direkt alıyoruz
        public async Task<IActionResult> Create(int bookId)
        {
            var http = _httpFactory.CreateClient();
            AddJwt(http);

            var userId = HttpContext.Session.GetInt32("userId");
            // Kullanıcı giriş yapmamışsa, Login sayfasına yönlendir.
            if (!userId.HasValue) return RedirectToAction("Index", "Login");

            // API'ye gönderilecek modeli oluşturuyoruz
            var model = new BorrowRecordCreateViewModel
            {
                BookId = bookId,
                UserId = userId.Value,
                // API'niz bunu gerektiriyorsa BorrowDate'i ekleyin
                BorrowDate = DateTime.Now
            };

            var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");

            var response = await http.PostAsync(_baseUrl + "BorrowRecords", content);

            if (response.IsSuccessStatusCode)
            {
                // ✅ Başarılı: Listenin yenilenmesi için TempData ile başarı mesajı gönder
                TempData["SuccessMessage"] = $"✅ Kitap başarıyla ödünç alındı. ";
            }
            else
            {
                // ❌ Hata Yönetimi
                var errorDetail = await response.Content.ReadAsStringAsync();
                string userMessage;

                // 1. Durum: Kitap Zaten Ödünç Alınmış Hatası Çevirisi
                // API'dan gelen hata detayında 'currently borrowed' ifadesini arıyoruz
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                    errorDetail.Contains("currently borrowed", StringComparison.OrdinalIgnoreCase))
                {
                    userMessage = "❌ Bu kitap  bir kullanıcı tarafından ödünç alınmıştır.";
                }
                // 2. Durum: Diğer API Hataları
                else
                {
                    // API'dan gelen diğer detayları da içeren genel hata mesajı
                    userMessage = $"❌ Ödünç alma işlemi başarısız oldu. API Durum Kodu: {(int)response.StatusCode}";
                }

                // Hata mesajını TempData'ya yaz
                TempData["ErrorMessage"] = userMessage;
            }

            // Başarılı veya hatalı olsun, kullanıcıyı Kitaplar listesine geri yönlendiriyoruz.
            // Bu yönlendirme, Books/index.cshtml'i yeniden yükler ve liste güncellenir.
            return RedirectToAction("Index", "Books");
        }
        // Library.UI.Controllers/BorrowRecordsController.cs

        // ============================================================
        // DELETE (POST) - Kayıt Silme
        // Hem Admin hem User'ın silme formunu karşılar.
        // ============================================================
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var http = _httpFactory.CreateClient();
            AddJwt(http);

            // API'ye DELETE isteği gönder
            var response = await http.DeleteAsync(_baseUrl + $"BorrowRecords/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Kayıt başarıyla silindi.";
            }
            else
            {
                string errorMessage = "Silme işlemi başarısız oldu.";

                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    errorMessage = "❌ Hata: Bu kaydı silme yetkiniz yok (Sadece Admin yetkisine sahipsiniz veya kendi kaydınız değil).";
                }
                else
                {
                    errorMessage = $"Silme işlemi başarısız oldu. Durum Kodu: {(int)response.StatusCode}";
                }
                TempData["ErrorMessage"] = errorMessage;
            }
            return RedirectToAction(nameof(Index));
        }
        // Library.UI.Controllers/BorrowRecordsController.cs

        // ============================================================
        // RETURN (GET) - İade İşlemini Başlatma
        // Admin, listedeki butona tıkladığında bu metot çağrılır.
        // ============================================================
        public async Task<IActionResult> ReturnBook(int id)
        {
            var http = _httpFactory.CreateClient();
            AddJwt(http);

            // API'ye PUT isteği gönder (API'deki Return/{id} metodu)
            var response = await http.PutAsync(_baseUrl + $"BorrowRecords/return/{id}", null);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Kitap başarıyla iade edildi.";
            }
            else
            {
                string errorMessage = "İade işlemi başarısız oldu.";

                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    errorMessage = "❌ Hata: Bu kaydı iade etme yetkiniz yok.";
                }
                else
                {
                    errorMessage = $"İade işlemi başarısız oldu. Durum Kodu: {(int)response.StatusCode}";
                }
                TempData["ErrorMessage"] = errorMessage;
            }
            return RedirectToAction(nameof(Index));
        }

    }
}