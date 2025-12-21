using Library.UI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Library.UI.Controllers
{
    public class HomeController : Controller
    {
        private readonly HttpClient _httpClient;

        public HomeController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            // API adresinizin (Port 7080) doğru olduğundan emin olun
            _httpClient.BaseAddress = new Uri("https://localhost:7080/api/");
        }

        public IActionResult Index()
        {
            var userRole = HttpContext.Session.GetString("role");
            if (userRole == "admin") return RedirectToAction("Index", "Admin");
            return RedirectToAction("Dashboard");
        }

        public async Task<IActionResult> Dashboard()
        {
            // ... (Önceki kodlar: Token alma, Rol kontrolü vb.) ...
            var token = HttpContext.Session.GetString("jwt");
            var role = HttpContext.Session.GetString("role");
            var userId = HttpContext.Session.GetInt32("userId");

            if (role == "admin") return RedirectToAction("Index", "Admin");
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Index", "Login");

            // API Adresini alıyoruz (Resimleri tamamlamak için lazım)
            string apiBaseUrl = "https://localhost:7080/";
            // Not: Burayı configuration'dan da çekebilirsiniz: _configuration["ApiSettings:BaseUrl"]

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var model = new UserDashboardViewModel();

            // API İSTEKLERİ (History endpointi kullanılıyor)
            var annTask = _httpClient.GetAsync("Announcements");
            var reqTask = _httpClient.GetAsync("Requests/me");
            var historyTask = _httpClient.GetAsync("BorrowRecords/me/history");
            var warningsTask = _httpClient.GetAsync("Users/me/warnings");

            await Task.WhenAll(annTask, reqTask, historyTask, warningsTask);

            // ... (Duyurular ve İstekler kısımları aynı kalacak) ...
            // 1. Duyurular
            if (annTask.Result.IsSuccessStatusCode)
            {
                var json = await annTask.Result.Content.ReadAsStringAsync();
                model.Announcements = JsonConvert.DeserializeObject<List<AnnouncementListViewModel>>(json)
                    ?.OrderByDescending(a => a.Date).Take(3).ToList();
            }

            // 2. İstekler
            if (reqTask.Result.IsSuccessStatusCode)
            {
                var json = await reqTask.Result.Content.ReadAsStringAsync();
                model.MyRequests = JsonConvert.DeserializeObject<List<RequestListViewModel>>(json)
                    ?.OrderByDescending(r => r.CreatedAt).Take(3).ToList();
            }

            // 3. AKTİF ÖDÜNÇLER
            if (historyTask.Result.IsSuccessStatusCode)
            {
                var json = await historyTask.Result.Content.ReadAsStringAsync();
                var allHistory = JsonConvert.DeserializeObject<List<BorrowRecordListViewModel>>(json);

                if (allHistory != null)
                {
                    // Sadece filtreleme ve sıralama yapıyoruz, URL'ye dokunmuyoruz.
                    model.ActiveBorrows = allHistory
                        .Where(b => !b.IsReturned)
                        .OrderBy(b => b.ReturnDate)
                        .ToList();
                }
            }

            // ... (Uyarılar ve return View kısmı aynı kalacak) ...
            if (warningsTask.Result.IsSuccessStatusCode)
            {
                var json = await warningsTask.Result.Content.ReadAsStringAsync();
                var warningsDto = JsonConvert.DeserializeObject<dynamic>(json);
                ViewData["UserWarnings"] = warningsDto;
            }

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}