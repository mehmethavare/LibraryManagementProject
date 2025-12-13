using Library.UI.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace Library.UI.Controllers
{
    public class UserRequestsController : Controller
    {
        private readonly HttpClient _httpClient;

        public UserRequestsController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            // API Portu 7080 olmalıdır (Swagger portunuz)
            _httpClient.BaseAddress = new Uri("https://localhost:7080/api/");
        }

        // ---------------------------------------------------------
        // KULLANICI TARAFI (USER) - İSTEKLERİMİ LİSTELE / MyRequests
        // ---------------------------------------------------------

        public async Task<IActionResult> MyRequests()
        {
            var token = HttpContext.Session.GetString("jwt");
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Oturum süreniz doldu, lütfen tekrar oturum açın.";
                return RedirectToAction("Index", "Login");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _httpClient.GetAsync("Requests/me");

            if (response.IsSuccessStatusCode)
            {
                var jsonData = await response.Content.ReadAsStringAsync();
                var requests = JsonConvert.DeserializeObject<List<RequestListViewModel>>(jsonData);
                return View(requests);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                TempData["Error"] = "Oturum süreniz doldu veya yetkiniz yok. Tekrar giriş yapın.";
                return RedirectToAction("Index", "Login");
            }

            TempData["Error"] = $"API Hatası: İstekler yüklenirken bir sorun oluştu. Durum Kodu: {(int)response.StatusCode}.";
            return View(new List<RequestListViewModel>());
        }

        [HttpGet]
        public IActionResult Create()
        {
            var token = HttpContext.Session.GetString("jwt");
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Index", "Login");
            return View();
        }

        // KULLANICI TARAFI - İSTEK GÖNDER / Create (POST)
        [HttpPost]
        public async Task<IActionResult> Create(CreateRequestViewModel model)
        {
            var token = HttpContext.Session.GetString("jwt");
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Index", "Login");

            // 1. Model Geçerlilik Kontrolü (UI tarafı validasyonu)
            if (!ModelState.IsValid)
            {
                // Eğer zorunlu alanlar boşsa veya kurallara uymuyorsa formu geri göster
                TempData["Error"] = "Lütfen tüm zorunlu alanları (Başlık, Açıklama vb.) doğru doldurunuz.";
                return View(model);
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var jsonContent = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

            // API Endpoint'i: Requests
            var response = await _httpClient.PostAsync("Requests", jsonContent);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "İsteğiniz başarıyla gönderildi.";
                return RedirectToAction("MyRequests");
            }

            // 2. API Hata İşleme Mantığı (Hatanın tam olarak ne olduğunu öğrenmek için)
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();

                // Hata mesajını daha okunabilir hale getirmek için.
                string errorMessage = "İstek Gönderimi Başarısız: Geçersiz veri.";

                // Detaylı JSON hata çözümleme
                if (response.Content.Headers.ContentType?.MediaType?.Contains("json") == true)
                {
                    try
                    {
                        var problemDetails = JsonConvert.DeserializeObject<Dictionary<string, object>>(errorContent);
                        if (problemDetails != null && problemDetails.ContainsKey("errors"))
                        {
                            // Hata detaylarını (validation errors) çek
                            // Not: Bu kısım API'nizin dönüş formatına bağlıdır.
                            var errors = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(problemDetails["errors"].ToString());
                            var allErrors = errors.SelectMany(x => x.Value);

                            if (allErrors.Any())
                            {
                                errorMessage = $"İstek Gönderimi Başarısız: {string.Join(" | ", allErrors.Distinct().Take(2))}";
                            }
                        }
                    }
                    catch { /* JSON çözümleme hatası olursa varsayılan mesaj kullanılır */ }
                }

                // Hata mesajını TempData yerine ModelState'e eklemek, View'da kalmasını sağlar.
                ModelState.AddModelError("", errorMessage);
            }
            else
            {
                ModelState.AddModelError("", $"API'den beklenmedik bir hata döndü. Durum Kodu: {(int)response.StatusCode}.");
            }

            // Başarısız olursa kullanıcıyı formuyla birlikte View'a geri gönder
            return View(model);
        }

        // ---------------------------------------------------------
        // ADMİN TARAFI - TÜM İSTEKLERİ LİSTELE / Index (İstek Kutusu)
        // ---------------------------------------------------------

        public async Task<IActionResult> Index()
        {
            var token = HttpContext.Session.GetString("jwt");
            var role = HttpContext.Session.GetString("role");

            if (string.IsNullOrEmpty(token) || role != "admin")
            {
                return RedirectToAction("Index", "Login");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // API'den istekleri çek
            var response = await _httpClient.GetAsync("Requests"); // GET: /api/Requests

            if (response.IsSuccessStatusCode)
            {
                var jsonData = await response.Content.ReadAsStringAsync();
                var requests = JsonConvert.DeserializeObject<List<RequestListViewModel>>(jsonData);
                return View(requests);
            }

            // Eğer 404 (Bulunamadı) veya başka bir hata alınırsa
            TempData["Error"] = $"İstek Kutusu Yüklenemedi. Durum Kodu: {(int)response.StatusCode}. Lütfen API'nin çalıştığından emin olun.";

            // 404 hatasını direkt Login'e yönlendirmemek için View döner
            return View(new List<RequestListViewModel>());
        }

        // UserRequestsController.cs dosyasındaki [HttpPost] UpdateStatus metodunu aşağıdaki kodla değiştirin:

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(UpdateRequestStatusViewModel model)
        {
            var token = HttpContext.Session.GetString("jwt");
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Index", "Login");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // API'nin zorunlu tuttuğu 'Message' alanını ekliyoruz
            var updateDto = new
            {
                Status = model.Status,
                AdminResponse = model.AdminResponse,
                Message = ""
            };

            var jsonContent = new StringContent(JsonConvert.SerializeObject(updateDto), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"Requests/{model.Id}/status", jsonContent);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "İstek durumu güncellendi.";
            }
            else
            {
                // 400 Bad Request Hata Mesajı Ayrıştırma (Parsing)
                string errorMessage = $"Güncelleme başarısız oldu. Durum Kodu: {(int)response.StatusCode}";

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();

                    // JSON'u ayrıştırıp sadece Türkçe hata mesajını çekme girişimi
                    try
                    {
                        var problemDetails = JsonConvert.DeserializeObject<Dictionary<string, object>>(errorContent);

                        if (problemDetails != null && problemDetails.ContainsKey("errors"))
                        {
                            // 'errors' alanındaki validasyon mesajlarını çekme
                            var errors = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(problemDetails["errors"].ToString());

                            // Tüm hataları birleştir ve temizle
                            var allErrors = errors.SelectMany(x => x.Value).Where(s => !string.IsNullOrWhiteSpace(s));

                            if (allErrors.Any())
                            {
                                // Sadece ilk hatayı göster (daha temiz)
                                errorMessage = $"Güncelleme Başarısız: {allErrors.First()}";
                            }
                            else
                            {
                                errorMessage = "Güncelleme Başarısız: Geçersiz veri gönderildi.";
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // Eğer JSON çözülemezse ham içeriği göster
                        errorMessage = $"Güncelleme Başarısız (400): {errorContent}";
                    }
                }

                TempData["Error"] = errorMessage;
            }

            return RedirectToAction("Index");
        }
    }
}