using Library.UI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

namespace Library.UI.Controllers
{
    public class UsersController : Controller
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _configuration;

        public UsersController(IHttpClientFactory httpFactory, IConfiguration configuration)
        {
            _httpFactory = httpFactory;
            _configuration = configuration;
        }

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

        // LIST (INDEX)
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("role") != "admin") return RedirectToAction("Index", "Login");

            var client = GetClientWithToken();
            if (client == null) return RedirectToAction("Index", "Login");

            try
            {
                var response = await client.GetAsync("Users");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var list = JsonSerializer.Deserialize<List<UserListViewModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return View(list);
                }
            }
            catch (Exception)
            {
                TempData["Error"] = "Sunucuya bağlanılamadı.";
            }

            return View(new List<UserListViewModel>());
        }

        // DETAILS
        public async Task<IActionResult> Details(int id)
        {
            var client = GetClientWithToken();
            if (client == null) return RedirectToAction("Index", "Login");

            var response = await client.GetAsync($"Users/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var model = JsonSerializer.Deserialize<UserUpdateViewModel>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return View(model);
            }

            TempData["Error"] = "Kullanıcı bulunamadı.";
            return RedirectToAction("Index");
        }

        // CREATE
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = GetClientWithToken();
            if (client == null) return RedirectToAction("Index", "Login");

            var response = await client.PostAsJsonAsync("Users", model);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Kullanıcı başarıyla oluşturuldu.";
                return RedirectToAction("Index");
            }

            TempData["Error"] = "Kullanıcı oluşturulurken bir hata oluştu.";
            return View(model);
        }

        // EDIT
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = GetClientWithToken();
            if (client == null) return RedirectToAction("Index", "Login");

            var response = await client.GetAsync($"Users/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var model = JsonSerializer.Deserialize<UserUpdateViewModel>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return View(model);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(UserUpdateViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = GetClientWithToken();
            if (client == null) return RedirectToAction("Index", "Login");

            // --- GÜVENLİK KONTROLÜ BAŞLANGICI ---

            // 1. Önce kullanıcının ŞU ANKİ (Veritabanındaki) halini çekelim
            // Çünkü model.Role yeni seçilen roldür, eski rolünü bilmiyoruz.
            var currentResp = await client.GetAsync($"Users/{model.Id}");
            if (currentResp.IsSuccessStatusCode)
            {
                var jsonString = await currentResp.Content.ReadAsStringAsync();
                var currentUserDb = JsonSerializer.Deserialize<UserUpdateViewModel>(jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // Eğer kullanıcı ŞU AN Admin ise ve YENİ ROLÜ Admin değilse (yani yetkisi alınıyorsa)
                if (currentUserDb != null && currentUserDb.Role == "Admin" && model.Role != "Admin")
                {
                    // 2. Sistemdeki toplam Admin sayısını kontrol etmeliyiz
                    var listResp = await client.GetAsync("Users");
                    if (listResp.IsSuccessStatusCode)
                    {
                        var listJson = await listResp.Content.ReadAsStringAsync();
                        var allUsers = JsonSerializer.Deserialize<List<UserListViewModel>>(listJson,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        var adminCount = allUsers?.Count(x => x.Role == "Admin") ?? 0;

                        if (adminCount <= 1)
                        {
                            TempData["SweetError"] = "Sistemde en az 1 tane Yönetici kalmalıdır!";

                            model.Role = "Admin";
                            return View(model);
                        }
                    }
                }
            }


            var response = await client.PutAsJsonAsync($"Users/{model.Id}", model);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Kullanici bilgileri guncellendi.";
                return RedirectToAction("Index");
            }

            TempData["Error"] = "Güncelleme başarısız.";
            return View(model);
        }

        // DELETE
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var client = GetClientWithToken();
            if (client == null) return RedirectToAction("Index", "Login");

            var response = await client.GetAsync($"Users/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var model = JsonSerializer.Deserialize<UserListViewModel>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return View(model);
            }
            return RedirectToAction("Index");
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var client = GetClientWithToken();
            if (client == null) return RedirectToAction("Index", "Login");

            var response = await client.DeleteAsync($"Users/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Kullanıcı silindi.";
            }
            else
            {
                TempData["Error"] = "Silme işlemi sırasında hata oluştu.";
            }

            return RedirectToAction("Index");
        }

        // --- İSTEDİĞİN ŞİFRE DEĞİŞTİRME METODU (API İLE KONUŞUR) ---
        // UsersController.cs dosyasının en altına bu metodu ekleyin:

        [HttpPost]
        public async Task<IActionResult> AdminChangePassword(string userId, string newPassword)
        {
            // 1. Client oluştur (API ile konuşmak için)
            var client = GetClientWithToken();
            if (client == null) return RedirectToAction("Index", "Login");

            // 2. API'ye gönderilecek paketi hazırla
            var passwordData = new
            {
                UserId = userId,
                NewPassword = newPassword
            };

            // 3. API'ye "Şifreyi Değiştir" isteği at
            // NOT: API tarafında [HttpPost("change-password")] metodu olmalı.
            var response = await client.PostAsJsonAsync("Users/change-password", passwordData);

            // 4. Sonuca göre mesaj ver
            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Şifre başarıyla güncellendi.";
            }
            else
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                TempData["Error"] = "Hata: " + errorMsg;
            }

            // ÖNEMLİ: İşlem bitince tekrar AYNI kullanıcının düzenleme sayfasına dönüyoruz
            return RedirectToAction("Edit", new { id = userId });
        }

    }

}