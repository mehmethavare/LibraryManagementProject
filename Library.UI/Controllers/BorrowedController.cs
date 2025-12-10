using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Library.UI.Controllers
{
    public class BorrowedController : Controller
    {
        private readonly HttpClient _httpClient;
        // API Base URL'nizi buraya doğru şekilde yazın
        private readonly string _apiBaseUrl = "https://localhost:7001/api";

        public BorrowedController(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_apiBaseUrl);
        }

        // "Evet, Ödünç Al" butonuna basılınca burası çalışacak
        [HttpPost]
        public async Task<IActionResult> Create(int bookId)
        {
            // Kullanıcı ID'sini Authentication/Authorization sisteminizden alın
            // Şimdilik varsayılan bir kullanıcı ID'si kullanıyoruz.
            int currentUserId = 1;

            // API'deki ödünç alma endpoint'ine POST isteği gönderme yolu
            var requestUrl = $"/borrow/{bookId}?userId={currentUserId}";

            try
            {
                // API'ye asenkron olarak POST isteği gönder
                var response = await _httpClient.PostAsync(requestUrl, null);

                if (response.IsSuccessStatusCode)
                {
                    // ✅ BAŞARILI DURUM: API 200/201 döndürdü

                    // Başarı mesajını TempData ile UI'a taşı
                    TempData["SuccessMessage"] = "Kitap başarıyla ödünç alındı ve listenize eklendi!";

                    // İstenen davranış: Kullanıcıyı "Borrowed" menüsüne yönlendir
                    // Not: Bu aksiyonun (Borrowed) ve Controller'ın (örneğin BooksController) var olduğunu varsayıyoruz.
                    return RedirectToAction("Borrowed", "Books");
                }
                else
                {
                    // ❌ HATA DURUMU: API 400 (Bad Request) gibi bir hata döndürdü
                    string errorMessage = "Ödünç alma işlemi başarısız oldu. Lütfen tekrar deneyin.";

                    // Eğer API'den gelen bir hata detayı varsa onu yakala
                    var content = await response.Content.ReadAsStringAsync();

                    // Not: Bu kısım, API'nizin hata yanıtının yapısına göre ayarlanmalıdır. 
                    // Eğer API'niz Türkçe hata mesajını (örn. "Bu kitap ödünçtedir") JSON içinde gönderiyorsa, onu parse edebilirsiniz.
                    if (!string.IsNullOrEmpty(content) && content.Contains("ödünç"))
                    {
                        errorMessage = "Hata: Bu kitap şu anda başka bir kullanıcı tarafından ödünç alınmıştır.";
                    }

                    // Hata mesajını UI'a taşı
                    TempData["ErrorMessage"] = errorMessage;

                    // Başarısız işlemde Kitaplar listesine geri yönlendir
                    return RedirectToAction("Index", "Books");
                }
            }
            catch (Exception ex)
            {
                // 🛑 İSTİSNA DURUMU: Ağ hatası veya API erişim sorunu
                TempData["ErrorMessage"] = $"Bağlantı hatası oluştu: {ex.Message}";
                return RedirectToAction("Index", "Books");
            }
        }
    }
}