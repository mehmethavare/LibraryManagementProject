using Library.UI.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.UI.Controllers
{
    // Duyurular sadece Admin tarafından yönetilir, bu yüzden rol kontrolü önemlidir.
    public class AnnouncementsController : Controller
    {
        private readonly HttpClient _httpClient;

        public AnnouncementsController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7080/api/");
        }

        // ---------------------------------------------------------
        // LİSTELEME: Herkes Görebilir (Admin + User)
        // ---------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var token = HttpContext.Session.GetString("jwt");
            // var role = HttpContext.Session.GetString("role"); // Artık rolu burada kontrol etmemize gerek yok

            // Sadece giriş yapılmış olması yeterli (Admin zorunluluğunu kaldırdık)
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Index", "Login");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // ... (Geri kalan kodlar aynı kalacak: API isteği ve Model döndürme) ...
            var response = await _httpClient.GetAsync("Announcements");
            var model = new List<AnnouncementListViewModel>();

            if (response.IsSuccessStatusCode)
            {
                var jsonData = await response.Content.ReadAsStringAsync();
                model = JsonConvert.DeserializeObject<List<AnnouncementListViewModel>>(jsonData)
                                    ?.OrderByDescending(a => a.Date)
                                    .ToList() ?? new List<AnnouncementListViewModel>();
            }
            else
            {
                TempData["Error"] = $"Duyurular yüklenemedi. Durum Kodu: {(int)response.StatusCode}";
            }

            return View(model);
        }

        // ---------------------------------------------------------
        // ADMİN: Yeni Duyuru Ekleme (POST)
        // ---------------------------------------------------------
        // AnnouncementsController.cs - Create Metodu 

        // AnnouncementsController.cs - Create Metodu (Otomatik Çıkış Eklendi)

        [HttpPost]
        public async Task<IActionResult> Create(CreateAnnouncementViewModel model)
        {
            var token = HttpContext.Session.GetString("jwt");
            var role = HttpContext.Session.GetString("role");

            if (string.IsNullOrEmpty(token) || role != "admin")
            {
                return RedirectToAction("Index", "Login");
            }

            // 🚨 KRİTİK DÜZELTME: ModelState Kontrolü ve Detaylı Hata Listeleme
            if (!ModelState.IsValid)
            {
                // ModelState içindeki tüm hataları topla ve okunabilir hale getir
                var errors = ModelState.Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value?.Errors.Select(e => new
                    {
                        Key = x.Key.Replace("model.", ""), // 'model.' ön ekini kaldır
                        ErrorMessage = e.ErrorMessage
                    }))
                    .ToList();

                if (errors.Any())
                {
                    var errorMessage = new System.Text.StringBuilder("Duyuru yayınlanamadı. Hata detayları:<ul>");

                    if (errors.Any(e => string.IsNullOrEmpty(e.Key)))
                    {
                        errorMessage.Append($"<li>Model Bağlama Hatası veya Anti-Forgery Token hatası. (Hata: {string.Join(", ", errors.Where(e => string.IsNullOrEmpty(e.Key)).Select(e => e.ErrorMessage))})</li>");
                    }

                    foreach (var error in errors.Where(e => !string.IsNullOrEmpty(e.Key)))
                    {
                        errorMessage.Append($"<li><b>{error.Key}</b> alanı hatalı: {error.ErrorMessage}</li>");
                    }
                    errorMessage.Append("</ul>");

                    TempData["Error"] = errorMessage.ToString();
                }
                else
                {
                    TempData["Error"] = "Form verileri beklenmedik bir şekilde boş geldi. Model Binding veya form adlarını kontrol edin.";
                }

                // Gerekli listeyi tekrar çekme (hata olsa bile)
                var response = await _httpClient.GetAsync("Announcements");
                var listModel = new List<AnnouncementListViewModel>();

                if (response.IsSuccessStatusCode)
                {
                    var jsonData = await response.Content.ReadAsStringAsync();
                    listModel = JsonConvert.DeserializeObject<List<AnnouncementListViewModel>>(jsonData)?.OrderByDescending(a => a.Date).ToList() ?? new List<AnnouncementListViewModel>();
                }

                ViewData["CreateModel"] = model;
                return View("Index", listModel);
            }

            // --- Başarılı Validasyon Sonrası API İsteği ---

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var jsonContent = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

            var responseAPI = await _httpClient.PostAsync("Announcements", jsonContent);

            if (responseAPI.IsSuccessStatusCode)
            {
                TempData["Success"] = "Duyuru başarıyla yayımlandı!";
            }
            // 🚨 YENİ EKLENTİ: HESAP KİLİTLENME/SİLİNME KONTROLÜ
            else if (responseAPI.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // API, kullanıcının kilitli/silinmiş olduğu için 401 döndürdü.
                HttpContext.Session.Clear(); // Oturumu sonlandır
                TempData["Error"] = "Hesabınız kilitlenmiş veya silinmiştir. Oturumunuz sonlandırıldı.";
                return RedirectToAction("Index", "Login");
            }
            else if (responseAPI.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var errorContent = await responseAPI.Content.ReadAsStringAsync();
                TempData["Error"] = $"API Validasyon Hatası: {errorContent}";
            }
            else
            {
                TempData["Error"] = $"Beklenmedik bir hata oluştu. Durum Kodu: {(int)responseAPI.StatusCode}";
            }

            return RedirectToAction("Index");
        }

        // ---------------------------------------------------------
        // ADMİN: Duyuru Silme (DELETE)
        // ---------------------------------------------------------

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var token = HttpContext.Session.GetString("jwt");
            var role = HttpContext.Session.GetString("role");

            // Yetki kontrolü (Sadece Admin silebilir)
            if (string.IsNullOrEmpty(token) || role != "admin")
            {
                TempData["Error"] = "Bu işlem için yetkiniz yok.";
                return RedirectToAction("Index", "Login");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // API Endpoint: DELETE /api/Announcements/{id}
            var response = await _httpClient.DeleteAsync($"Announcements/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Duyuru başarıyla silindi.";
            }
            else
            {
                // Hata durumunda kullanıcıyı bilgilendir
                TempData["Error"] = $"Silme işlemi başarısız oldu. Durum Kodu: {(int)response.StatusCode}";
            }

            return RedirectToAction("Index");
        }
    }
}