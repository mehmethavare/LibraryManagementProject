using Library.UI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

namespace Library.UI.Controllers
{
    public class BorrowRecordsController : BaseController
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly string _baseUrl;

        public BorrowRecordsController(IHttpClientFactory httpFactory, IConfiguration config)
        {
            _httpFactory = httpFactory;
            _baseUrl = config["ApiSettings:BaseUrl"] ?? "https://localhost:7080/api/";
            if (!_baseUrl.EndsWith("/")) _baseUrl += "/";
        }

        // LISTELEME (INDEX)
        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("role");
            var userId = HttpContext.Session.GetInt32("userId");

            var http = _httpFactory.CreateClient();
            AddJwt(http);

            // 1. Önce Ana Listeyi Çek (Tüm kayıtlar)
            string apiEndpoint;
            if (role == "admin")
                apiEndpoint = _baseUrl + "BorrowRecords";
            else if (role == "user" && userId.HasValue)
                apiEndpoint = _baseUrl + "BorrowRecords/me/history";
            else
                return RedirectToAction("Index", "Login");

            var response = await http.GetAsync(apiEndpoint);
            var mainList = new List<BorrowRecordListViewModel>();

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                mainList = JsonSerializer.Deserialize<List<BorrowRecordListViewModel>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new List<BorrowRecordListViewModel>();
            }

            // Index metodu içindeki role == "admin" bloğunun içini şu şekilde güncelleyin:
            if (role == "admin")
            {
                // Bekleyen talepleri çek
                var pendingResponse = await http.GetAsync(_baseUrl + "BorrowRecords/return-requests/pending");
                // Reddedilen talepleri de çek (Eğer API'nizde böyle bir endpoint varsa)
                // Yoksa bile API'den gelen mainList içindeki ReturnRequestStatus zaten 3 ise View bunu gösterecektir.

                if (pendingResponse.IsSuccessStatusCode)
                {
                    var jsonPending = await pendingResponse.Content.ReadAsStringAsync();
                    var pendingList = JsonSerializer.Deserialize<List<BorrowRecordListViewModel>>(jsonPending,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (pendingList != null)
                    {
                        var pendingIds = pendingList.Select(x => x.Id).ToHashSet();
                        foreach (var item in mainList)
                        {
                            if (pendingIds.Contains(item.Id))
                            {
                                item.ReturnRequestStatus = 1; // Bekliyor
                            }
                        }
                    }
                }
            }

            return View(mainList);
        }

        // ÖDÜNÇ ALMA (CREATE)
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
                TempData["SuccessMessage"] = "✅ Kitap başarıyla ödünç alındı.";
            else
                TempData["ErrorMessage"] = "❌ Ödünç alma başarısız oldu.";

            return RedirectToAction("Index", "Books");
        }

        // ============================================================
        // YENİ AKIŞ: İADE İŞLEMLERİ
        // ============================================================

        // 1. KULLANICI: İade Talebi Oluştur (User -> Request)
        [HttpPost]
        public async Task<IActionResult> RequestReturn(int id)
        {
            var http = _httpFactory.CreateClient();
            AddJwt(http);

            // API Endpoint: POST api/BorrowRecords/{id}/return-request
            var response = await http.PostAsync(_baseUrl + $"BorrowRecords/{id}/return-request", null);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "İade talebiniz alındı. Admin onayı bekleniyor.";
            }
            else
            {
                var msg = await response.Content.ReadAsStringAsync();
                TempData["ErrorMessage"] = $"Talep oluşturulamadı: {msg}";
            }

            return RedirectToAction(nameof(Index));
        }

        // 2. ADMIN: İadeyi Onayla (Admin -> Approve)
        [HttpPost]
        public async Task<IActionResult> ApproveReturn(int id)
        {
            var role = HttpContext.Session.GetString("role");
            if (role != "admin") return RedirectToAction("Index", "Login");

            var http = _httpFactory.CreateClient();
            AddJwt(http);

            // API Endpoint: PUT api/BorrowRecords/{id}/approve-return
            var response = await http.PutAsync(_baseUrl + $"BorrowRecords/{id}/approve-return", null);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "İade onaylandı ve kitap teslim alındı.";
            }
            else
            {
                TempData["ErrorMessage"] = "Onaylama işlemi başarısız.";
            }

            return RedirectToAction(nameof(Index));
        }

        // 3. ADMIN: İadeyi Reddet (Admin -> Reject)
        [HttpPost]
        public async Task<IActionResult> RejectReturn(int id)
        {
            var role = HttpContext.Session.GetString("role");
            if (role != "admin") return RedirectToAction("Index", "Login");

            var http = _httpFactory.CreateClient();
            AddJwt(http);

            // API Endpoint: PUT api/BorrowRecords/{id}/reject-return
            var response = await http.PutAsync(_baseUrl + $"BorrowRecords/{id}/reject-return", null);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Talep reddedildi. Kullanıcıya ceza puanı işlendi.";
            }
            else
            {
                TempData["ErrorMessage"] = "Reddetme işlemi başarısız.";
            }

            return RedirectToAction(nameof(Index));
        }

        // SİLME (DELETE)
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var http = _httpFactory.CreateClient();
            AddJwt(http);

            var response = await http.DeleteAsync(_baseUrl + $"BorrowRecords/{id}");

            if (response.IsSuccessStatusCode)
                TempData["SuccessMessage"] = "Kayıt silindi.";
            else
                TempData["ErrorMessage"] = "Silme başarısız.";

            return RedirectToAction(nameof(Index));
        }
    }
}