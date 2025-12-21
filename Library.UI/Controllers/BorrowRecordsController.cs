using Library.UI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;

namespace Library.UI.Controllers
{
    // 🔥 ÖNEMLİ: AuthFilter (Kilitli Hesap Kontrolü) için BaseController'dan miras alıyoruz
    public class BorrowRecordsController : BaseController
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly string _baseUrl;

        public BorrowRecordsController(IHttpClientFactory httpFactory, IConfiguration config)
        {
            _httpFactory = httpFactory;
            // API adresini Config'den al, yoksa varsayılanı kullan
            _baseUrl = config["ApiSettings:BaseUrl"] ?? "https://localhost:7080/api/";
            if (!_baseUrl.EndsWith("/")) _baseUrl += "/";
        }

        // ============================================================
        // INDEX (Listeleme)
        // ============================================================
        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("role");
            var userId = HttpContext.Session.GetInt32("userId");

            var http = _httpFactory.CreateClient();
            AddJwt(http); // Token ekle (BaseController'dan gelir)

            string apiEndpoint;

            // Rol tabanlı endpoint seçimi
            if (role == "admin")
            {
                // Admin: Herkesi gör
                apiEndpoint = _baseUrl + "BorrowRecords";
            }
            else if (role == "user" && userId.HasValue)
            {
                // User: Sadece kendini gör
                apiEndpoint = _baseUrl + "BorrowRecords/me/history";
            }
            else
            {
                TempData["ErrorMessage"] = "Oturum bilgileri eksik. Lütfen tekrar giriş yapın.";
                return RedirectToAction("Index", "Login");
            }

            var response = await http.GetAsync(apiEndpoint);

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = $"Liste yüklenemedi. Kod: {(int)response.StatusCode}";
                return View(new List<BorrowRecordListViewModel>());
            }

            var json = await response.Content.ReadAsStringAsync();

            try
            {
                var list = JsonSerializer.Deserialize<List<BorrowRecordListViewModel>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return View(list ?? new List<BorrowRecordListViewModel>());
            }
            catch
            {
                TempData["ErrorMessage"] = "Veri işlenirken hata oluştu.";
                return View(new List<BorrowRecordListViewModel>());
            }
        }

        // ============================================================
        // CREATE (POST) - Ödünç Alma
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> Create(int bookId)
        {
            var http = _httpFactory.CreateClient();
            AddJwt(http);

            var userId = HttpContext.Session.GetInt32("userId");
            if (!userId.HasValue) return RedirectToAction("Index", "Login");

            var model = new BorrowRecordCreateViewModel
            {
                BookId = bookId,
                UserId = userId.Value,
                BorrowDate = DateTime.Now
            };

            var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await http.PostAsync(_baseUrl + "BorrowRecords", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "✅ Kitap başarıyla ödünç alındı.";
            }
            else
            {
                var errorDetail = await response.Content.ReadAsStringAsync();

                // Kullanıcı dostu hata mesajı
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                    errorDetail.Contains("currently borrowed", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] = "❌ Bu kitap şu anda başkası tarafından alınmış.";
                }
                else
                {
                    TempData["ErrorMessage"] = "❌ Ödünç alma başarısız oldu.";
                }
            }

            return RedirectToAction("Index", "Books");
        }

        // ============================================================
        // RETURN (POST) - İade ve Kırmızı Alarm Kontrolü
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> ReturnBook(int id, string dueDateStr)
        {
            // NOT: Tarihi string olarak alıyoruz (yyyy-MM-dd HH:mm:ss formatında gelmeli)
            // Bu sayede View ile Controller arasındaki format uyuşmazlığını önlüyoruz.

            var http = _httpFactory.CreateClient();
            AddJwt(http);

            // 1. Tarihi Parse Et
            DateTime dueDate;
            bool isDateValid = DateTime.TryParse(dueDateStr, out dueDate);

            // 2. API'ye İade İsteği At
            var response = await http.PutAsync(_baseUrl + $"BorrowRecords/return/{id}", null);

            if (response.IsSuccessStatusCode)
            {
                // 3. GEÇ İADE KONTROLÜ
                // Eğer tarih geçerliyse VE Şu an > Teslim Tarihi ise -> GEÇ KALINDI
                if (isDateValid && DateTime.Now > dueDate)
                {
                    // 🚨 KIRMIZI ALARM (Layout'taki SweetAlert bunu yakalar)
                    TempData["LateReturnAlert"] = "<b>⚠️ GEÇ İADE TESPİT EDİLDİ!</b><br><br>" +
                                                  "Kitabı süresi dolduktan sonra iade ettiniz.<br>" +
                                                  "Uyarı puanınız arttırıldı. (3. uyarıda hesabınız kilitlenir).";
                }
                else
                {
                    // 🟢 YEŞİL MESAJ
                    TempData["SuccessMessage"] = "Kitap zamanında iade edildi. Teşekkürler!";
                }
            }
            else
            {
                string msg = "İade işlemi başarısız.";
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    msg = "❌ Yetkiniz yok.";

                TempData["ErrorMessage"] = msg;
            }

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // DELETE (POST) - Kayıt Silme
        // ============================================================
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var http = _httpFactory.CreateClient();
            AddJwt(http);

            var response = await http.DeleteAsync(_baseUrl + $"BorrowRecords/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Kayıt başarıyla silindi.";
            }
            else
            {
                TempData["ErrorMessage"] = "Silme işlemi başarısız. Yetkiniz olmayabilir.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}