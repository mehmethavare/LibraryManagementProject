using Library.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text.Json; // System.Text.Json, Login'de kullanıldığı için kalmalıdır.
using System.Linq; // SelectMany için gereklidir
using System.Collections.Generic; // Dictionary için gereklidir

namespace Library.UI.Controllers
{
    [AllowAnonymous]
    public class LoginController : Controller
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly string _baseUrl;

        public LoginController(IHttpClientFactory httpFactory, IConfiguration config)
        {
            _httpFactory = httpFactory;
            _baseUrl = config["ApiBaseUrl"] ?? "https://localhost:7080/api/";
        }

        // --- YARDIMCI METOT: HttpClient Oluşturma ---
        private HttpClient GetClient()
        {
            var client = _httpFactory.CreateClient();
            client.BaseAddress = new Uri(_baseUrl.EndsWith("/") ? _baseUrl : _baseUrl + "/");
            return client;
        }

        // --- YARDIMCI METOT: FOTOĞRAF URL'SİNİ ÇEKME (API'DEN) ---
        private async Task<string?> GetProfileImageUrl(string token)
        {
            var client = GetClient(); // GetClient() metodu BaseUrl'ü zaten ayarlar
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await client.GetAsync("Users/me");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var profileResult = System.Text.Json.JsonDocument.Parse(json).RootElement;

                    if (profileResult.TryGetProperty("profileImageId", out var imageIdElement) && imageIdElement.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        var imageId = imageIdElement.GetString();

                        if (!string.IsNullOrEmpty(imageId))
                        {
                            // Temel URL'yi zaten GetClient() ayarladığı için _baseUrl'ü tekrar kontrol etmeye gerek yok.
                            // Ancak kontrol etmek isterseniz parantez şart.

                            // Base URL'nin sonunda slash olmadığından emin olarak URL'yi oluşturuyoruz
                            var baseUrlClean = _baseUrl.EndsWith("/") ? _baseUrl : _baseUrl + "/";
                            return $"{baseUrlClean}Files/{imageId}";
                        }
                    }
                }
            }
            catch { /* Hata durumunda null döndürür */ }
            return null;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("jwt")))
            {
                return RedirectToAction("Index", "Home");
            }
            return View(new LoginRequestViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Index(LoginRequestViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var http = GetClient();
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await http.PostAsync("Auth/login", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(errorMessage))
                {
                    errorMessage = "Email veya şifre hatalı.";
                }
                else
                {
                    errorMessage = errorMessage.Trim('"');
                }

                ModelState.AddModelError("", errorMessage);
                return View(model);
            }

            var body = await response.Content.ReadAsStringAsync();
            var result = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(body,
              new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result != null && result.User != null)
            {
                var token = result.Token;

                // Session kayıtları
                HttpContext.Session.SetInt32("userId", result.User.Id);
                HttpContext.Session.SetString("jwt", token);
                HttpContext.Session.SetString("username", result.User.Email);
                HttpContext.Session.SetString("role", result.User.Role.ToLower());

                // KRİTİK ÇÖZÜM: API'den en güncel profil fotoğrafı URL'sini çek ve Session'a kaydet
                var profileImageUrl = await GetProfileImageUrl(token);

                // Bu Session kaydı, Layout ve Profil sayfalarının fotoğrafı giriş anında görmesini sağlar.
                HttpContext.Session.SetString("profileImageUrl", profileImageUrl ?? "");

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Sunucudan hatalı veri döndü.");
            return View(model);
        }

        [AllowAnonymous]
        public IActionResult Logout()
        {
            // Çıkış yapınca her şeyi sil
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Login");
        }

        private class LoginResponse
        {
            public string Token { get; set; } = string.Empty;
            public LoginUser User { get; set; } = new();
        }

        private class LoginUser
        {
            public int Id { get; set; }
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = "User";
            public bool IsLocked { get; set; }
        }

        // [HttpGet] Kayıt formunu gösterir
        [HttpGet]
        public IActionResult Register()
        {
            // Kayıt formunu görüntüler
            return View();
        }

        // [HttpPost] Kayıt formunu işler ve API'ye gönderir
        [HttpPost]
        public async Task<IActionResult> Register(RegisterRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client = GetClient();

            try
            {
                // 🚨 API'nin beklediği RegisterRequestDto'ya TAM UYAN anonim nesneyi oluşturuyoruz:
                var apiRequestData = new
                {
                    model.Name,
                    model.Surname,
                    model.Email,
                    model.PhoneNumber, // Null veya değer içerir
                    model.Password
                };

                var jsonContent = JsonConvert.SerializeObject(apiRequestData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // API Endpoint'i: /api/Auth/register
                var response = await client.PostAsync("Auth/register", content);

                if (response.IsSuccessStatusCode)
                {
                    // Kayıt başarılı ise kullanıcıyı Login sayfasına yönlendir.
                    TempData["Success"] = "Kayıt işlemi başarıyla tamamlandı. Giriş yapabilirsiniz.";
                    return RedirectToAction("Index", "Login");
                }
                else
                {
                    // --- GELİŞTİRİLMİŞ HATA İŞLEME MANTIĞI (400 Bad Request için) ---
                    string errorMessage = "Kayıt işlemi başarısız oldu. Lütfen tekrar deneyin.";
                    var errorContent = await response.Content.ReadAsStringAsync();

                    if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        // API'den gelen validasyon hatasını ayrıştır
                        try
                        {
                            var problemDetails = JsonConvert.DeserializeObject<Dictionary<string, object>>(errorContent);
                            if (problemDetails != null && problemDetails.ContainsKey("errors"))
                            {
                                var errors = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(problemDetails["errors"].ToString());
                                var allErrors = errors.SelectMany(x => x.Value).Where(s => !string.IsNullOrWhiteSpace(s));

                                if (allErrors.Any())
                                {
                                    // En anlaşılır hatayı göster (Örn: E-posta zaten kullanılıyor)
                                    errorMessage = $"Kayıt Başarısız: {allErrors.First()}";
                                }
                            }
                        }
                        catch (Newtonsoft.Json.JsonException) // 🚨 Hata Düzeltmesi Burada!
                        { /* Ayrıştırma hatası olursa genel mesaj kullanılır */ }
                    }
                    // ----------------------------------------------------------------------

                    TempData["Error"] = errorMessage;
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Bir hata oluştu: {ex.Message}";
                return View(model);
            }
        }
    }
}