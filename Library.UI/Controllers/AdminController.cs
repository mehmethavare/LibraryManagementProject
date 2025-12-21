using Library.UI.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Library.UI.Controllers
{
    public class AdminController : Controller
    {
        private readonly HttpClient _httpClient;
        // Resimler "api/" altında değil, kök dizin "Files/" altında olduğu için kök adresi ayrı tutuyoruz
        private readonly string _apiRootUrl = "https://localhost:7080/";

        public AdminController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri(_apiRootUrl + "api/");
        }

        public async Task<IActionResult> Index()
        {
            var token = HttpContext.Session.GetString("jwt");
            var role = HttpContext.Session.GetString("role");

            if (string.IsNullOrEmpty(token) || role != "admin")
            {
                return RedirectToAction("Index", "Login");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var model = new AdminDashboardViewModel();

            var bookTask = _httpClient.GetAsync("Books");
            var userTask = _httpClient.GetAsync("Users");
            var borrowTask = _httpClient.GetAsync("BorrowRecords");
            var requestTask = _httpClient.GetAsync("Requests");

            await Task.WhenAll(bookTask, userTask, borrowTask, requestTask);

            // 1. Kitap Sayısı
            if (bookTask.Result.IsSuccessStatusCode)
            {
                var json = await bookTask.Result.Content.ReadAsStringAsync();
                var list = JsonConvert.DeserializeObject<List<object>>(json);
                model.TotalBooks = list?.Count ?? 0;
            }

            // 2. Kullanıcı Sayısı
            if (userTask.Result.IsSuccessStatusCode)
            {
                var json = await userTask.Result.Content.ReadAsStringAsync();
                var list = JsonConvert.DeserializeObject<List<object>>(json);
                model.TotalUsers = list?.Count ?? 0;
            }

            // 3. AKTİF ÖDÜNÇLER (Resim URL Düzeltmesi Burada Yapılıyor)
            if (borrowTask.Result.IsSuccessStatusCode)
            {
                var json = await borrowTask.Result.Content.ReadAsStringAsync();
                var allBorrows = JsonConvert.DeserializeObject<List<BorrowRecordListViewModel>>(json);

                if (allBorrows != null)
                {
                    var activeList = allBorrows.Where(x => !x.IsReturned).ToList();

                    // --- URL DÜZELTME DÖNGÜSÜ ---
                    foreach (var item in activeList)
                    {
                        // Resim yolu var ama "http" ile başlamıyorsa (yani relative path ise: "Files/img.jpg")
                        if (!string.IsNullOrEmpty(item.CoverImageUrl) && !item.CoverImageUrl.StartsWith("http"))
                        {
                            // Başına https://localhost:7080/ ekle
                            item.CoverImageUrl = _apiRootUrl + item.CoverImageUrl;
                        }
                    }
                    // ----------------------------

                    model.ActiveBorrows = activeList.Count;
                    model.RecentActiveBorrows = activeList.OrderBy(x => x.BorrowDate).Take(5).ToList();
                }
            }

            // 4. İstekler
            if (requestTask.Result.IsSuccessStatusCode)
            {
                var json = await requestTask.Result.Content.ReadAsStringAsync();
                var requests = JsonConvert.DeserializeObject<List<RequestListViewModel>>(json);
                model.LatestRequests = requests?.OrderByDescending(r => r.CreatedAt).Take(5).ToList();
            }

            return View(model);
        }
    }
}