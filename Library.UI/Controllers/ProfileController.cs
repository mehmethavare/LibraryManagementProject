using Library.UI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration; // IConfiguration için gerekli
using System; // Exception için gerekli

namespace Library.UI.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _baseUrl;

        public ProfileController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _baseUrl = configuration["ApiBaseUrl"] ?? "https://localhost:7080/api/";
        }

        // --- YARDIMCI METOT: CLIENT OLUŞTURMA ---
        private HttpClient GetClient(string token)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Base URL'i tek bir yerde yönet
            var url = _baseUrl.EndsWith("/") ? _baseUrl : _baseUrl + "/";
            client.BaseAddress = new Uri(url);
            return client;
        }

        // ==========================================
        // YARDIMCI METOT: KULLANICIYI ÇEKME
        // ==========================================
        private async Task<UserUpdateViewModel?> GetUserProfile()
        {
            var token = HttpContext.Session.GetString("jwt");
            if (string.IsNullOrEmpty(token)) return null;

            // CLIENT ARTIK AYRI YARDIMCI METOTTA OLUŞTURULUYOR
            var client = GetClient(token);
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // API Endpoint: GET /api/Users/me
            var url = "Users/me";

            try
            {
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    var userProfile = JsonSerializer.Deserialize<UserUpdateViewModel>(json, jsonOptions);

                    if (userProfile != null)
                    {
                        // KRİTİK UI ÇÖZÜMÜ: Eğer API Profil URL'sini geri döndürmüyorsa,
                        // en son Session'a kaydettiğimiz URL'yi kullan.
                        var sessionImage = HttpContext.Session.GetString("profileImageUrl");

                        // Session doluysa, Modeldeki ProfileImageUrl alanını Session'daki güncel veriyle ezer.
                        if (!string.IsNullOrEmpty(sessionImage))
                        {
                            userProfile.ProfileImageUrl = sessionImage;
                        }
                    }

                    return userProfile;
                }
            }
            catch (Exception)
            {
                // Hata durumunda null döndür
            }

            return null;
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
                return RedirectToAction("Index", "Books");
            }

            ViewBag.UserRole = HttpContext.Session.GetString("role");
            ViewBag.UserEmail = HttpContext.Session.GetString("username");

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
        [HttpPost]
        public async Task<IActionResult> Edit(UserUpdateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ErrorMessage = "Lütfen tüm zorunlu alanları doldurun.";
                return View(model);
            }

            var token = HttpContext.Session.GetString("jwt");
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Index", "Login");

            // CLIENT ARTIK AYRI YARDIMCI METOTTA OLUŞTURULUYOR
            var client = GetClient(token);
            var url = "Users/me";

            try
            {
                var response = await client.PutAsJsonAsync(url, model);

                if (response.IsSuccessStatusCode)
                {
                    // KRİTİK UI ÇÖZÜMÜ: API Geri Vermediği İçin Session'a Yazma
                    if (!string.IsNullOrEmpty(model.ProfileImageUrl))
                    {
                        HttpContext.Session.SetString("profileImageUrl", model.ProfileImageUrl);
                    }

                    // IDE0059 uyarısını vermemek için API yanıt içeriğini okumuyoruz
                    // var _ = await response.Content.ReadAsStringAsync(); 

                    TempData["SuccessMessage"] = "Profiliniz başarıyla güncellendi.";

                    return RedirectToAction("Index");
                }
                else
                {
                    // API'den dönen hatayı okuyup kullanıcıya gösterelim
                    var errorMsg = await response.Content.ReadAsStringAsync();

                    ViewBag.ErrorMessage = "Güncelleme başarısız. Lütfen bilgileri kontrol edin. Detay: " + response.StatusCode;

                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Sunucu ile iletişim hatası. Lütfen daha sonra tekrar deneyin.";
                return View(model);
            }
        }
    }
}