using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // Session için gerekli

namespace Library.UI.Controllers
{
    public class HomeController : Controller
    {
        // Proje açýlýnca direkt Dashboard'a yönlendir
        public IActionResult Index()
        {
            return RedirectToAction("Dashboard");
        }

        // ÝÞTE EKSÝK OLAN KISIM BU:
        public IActionResult Dashboard()
        {
            // Kullanýcý giriþ yapmamýþsa Login sayfasýna at
            var role = HttpContext.Session.GetString("role");
            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Index", "Login");
            }

            // Rol bilgisini sayfaya gönder (Admin mi User mý?)
            ViewBag.UserRole = role;

            return View(); // Bu komut Views/Home/Dashboard.cshtml dosyasýný arar
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}