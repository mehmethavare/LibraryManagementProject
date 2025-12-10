namespace Library.UI.Models
{
    public class BookReviewUpdateViewModel
    {
        public string Comment { get; set; } = string.Empty;
        public int Rating { get; set; }
        public int Id { get; set; }

        // --- EKLENMESİ GEREKEN ALANLAR ---

        // API çağrısında PUT isteği için bu gerekli olabilir.
        public int BookId { get; set; }

        // Sadece kullanıcıya göstermek için. Geriye gönderilmesine gerek yok.
        // API'den gelen yanıta göre bu alanın gelmesi gerekir.
        public string BookTitle { get; set; }
    }
}
