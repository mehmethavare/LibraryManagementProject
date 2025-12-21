using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Library.UI.Filters
{
    // ÖNEMLİ: IAsyncActionFilter arayüzünü kullanıyoruz (API çağrısı için şart)
    public class AuthFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var controllerName = context.RouteData.Values["controller"]?.ToString();

            // 1. Login sayfasına her zaman izin ver (Sonsuz döngü olmasın)
            if (controllerName == "Login")
            {
                await next();
                return;
            }

            // 2. Session Kontrolü (Giriş yapılmış mı?)
            string? jwt = context.HttpContext.Session.GetString("jwt");
            string? role = context.HttpContext.Session.GetString("role");

            if (string.IsNullOrEmpty(jwt))
            {
                // Giriş yoksa Login'e at
                context.Result = new RedirectToActionResult("Index", "Login", null);
                return;
            }

            // =================================================================
            // 3. KRİTİK NOKTA: GÜNCEL KİLİT KONTROLÜ (API'ye Soruyoruz)
            // =================================================================
            var httpFactory = context.HttpContext.RequestServices.GetService<IHttpClientFactory>();
            var config = context.HttpContext.RequestServices.GetService<IConfiguration>();

            if (httpFactory != null && config != null)
            {
                var client = httpFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

                var baseUrl = config["ApiSettings:BaseUrl"] ?? "https://localhost:7080/api/";
                if (!baseUrl.EndsWith("/")) baseUrl += "/";

                try
                {
                    // Kullanıcının güncel durumunu çekiyoruz (DB'den IsLocked geldi mi?)
                    var response = await client.GetAsync(baseUrl + "Users/me");

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(json);

                        // "isLocked" true ise kullanıcıyı at!
                        if (doc.RootElement.TryGetProperty("isLocked", out var lockedProp) && lockedProp.GetBoolean())
                        {
                            context.HttpContext.Session.Clear(); // Oturumu sil

                            // Hata mesajı bırak (Login sayfasında görünür)
                            if (context.Controller is Controller controller)
                            {
                                controller.TempData["Error"] = "HESABINIZ KİLİTLENDİĞİ İÇİN OTURUMUNUZ SONLANDIRILDI.";
                            }

                            // Login'e fırlat
                            context.Result = new RedirectToActionResult("Index", "Login", null);
                            return;
                        }
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        // Token süresi dolmuşsa da at
                        context.HttpContext.Session.Clear();
                        context.Result = new RedirectToActionResult("Index", "Login", null);
                        return;
                    }
                }
                catch
                {
                    // API kapalıysa veya hata varsa güvenli tarafta kalıp devam ettiriyoruz
                    // İsterseniz burada da login'e atabilirsiniz.
                }
            }

            // 4. Yetki Kontrolü (Örn: User admin sayfasına girmesin)
            if (controllerName == "Users" && role != "admin")
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
                return;
            }

            // Her şey yolundaysa sayfayı aç
            await next();
        }
    }
}