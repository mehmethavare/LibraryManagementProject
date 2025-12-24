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
            var userCountTask = _httpClient.GetAsync("Users/total-active-count");
            var borrowTask = _httpClient.GetAsync("BorrowRecords");
            var requestTask = _httpClient.GetAsync("Requests");

            await Task.WhenAll(bookTask, userCountTask, borrowTask, requestTask);

            // 1. Kitap Sayısı
            if (bookTask.Result.IsSuccessStatusCode)
            {
                var json = await bookTask.Result.Content.ReadAsStringAsync();

                // BookViewModel yerine 'dynamic' kullanarak tip bağımlılığını kaldırıyoruz
                var allBooks = JsonConvert.DeserializeObject<List<dynamic>>(json);

                if (allBooks != null)
                {
                    model.TotalBooks = allBooks.Count;
                }
            }

            // 2. Kullanıcı Sayısı
            if (userCountTask.Result.IsSuccessStatusCode)
            {
                var countJson = await userCountTask.Result.Content.ReadAsStringAsync();
                if (int.TryParse(countJson, out int activeUserCount))
                {
                    model.TotalUsers = activeUserCount;
                }
            }

            // 3. AKTİF ÖDÜNÇLER
            if (borrowTask.Result.IsSuccessStatusCode)
            {
                var json = await borrowTask.Result.Content.ReadAsStringAsync();
                var allBorrows = JsonConvert.DeserializeObject<List<BorrowRecordListViewModel>>(json);

                if (allBorrows != null)
                {
                    var activeList = allBorrows.Where(x => !x.IsReturned).ToList();

                    foreach (var item in activeList)
                    {
                        if (!string.IsNullOrEmpty(item.CoverImageUrl) && !item.CoverImageUrl.StartsWith("http"))
                        {
                            item.CoverImageUrl = _apiRootUrl + item.CoverImageUrl;
                        }
                    }

                    model.ActiveBorrows = activeList.Count;
                    model.RecentActiveBorrows = activeList.OrderBy(x => x.BorrowDate).Take(5).ToList();
                }
            }
            // 4. İstekler (Admin Dashboard için sadece bekleyenleri getirir)
            if (requestTask.Result.IsSuccessStatusCode)
            {
                var json = await requestTask.Result.Content.ReadAsStringAsync();

                // API'den gelen veriyi liste olarak karşıla
                // Not: RequestListViewModel isminde hata alırsan AdminDashboardViewModel içindeki tip ile aynı yap.
                var requests = JsonConvert.DeserializeObject<List<RequestListViewModel>>(json);

                if (requests != null)
                {
                    // 🚨 FİLTRELEME: Zaten API'ye "dashboard-pending" eklediysen API filtrelenmiş yollayacaktır.
                    // Ama garanti olsun dersen burada da Status == 0 (Pending) kontrolü yapabilirsin.
                    model.LatestRequests = requests
                        .Where(r => r.Status == 0) // Eğer Status enum/int ise 0 'Pending'dir.
                        .OrderByDescending(r => r.CreatedAt)
                        .Take(5)
                        .ToList();
                }
            }

            return View(model);
        }
    }
}