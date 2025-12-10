using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Library.UI.Filters
{
    public class AuthFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var controller = context.RouteData.Values["controller"]?.ToString();
            var action = context.RouteData.Values["action"]?.ToString();

            // 1. LOGIN sayfasına her zaman izin ver (sonsuz döngüye girmesin)
            if (controller == "Login")
                return;

            // 2. Kullanıcı Giriş Yapmış mı Kontrolü (Session'da JWT var mı?)
            string? jwt = context.HttpContext.Session.GetString("jwt");
            string? role = context.HttpContext.Session.GetString("role");

            if (string.IsNullOrEmpty(jwt))
            {
                // Giriş yapmamışsa Login'e gönder
                context.Result = new RedirectToActionResult("Index", "Login", null);
                return;
            }

            // 3. YETKİ KONTROLÜ (Admin olmayanlar bazı sayfalara giremesin)
            // Eğer gidilen sayfa "Users" ise VE kullanıcının rolü "admin" değilse
            if (controller == "Users" && role != "admin")
            {
                // Yetkisiz girişi engelle ve Ana Sayfaya yönlendir
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Burası boş kalabilir
        }
    }
}