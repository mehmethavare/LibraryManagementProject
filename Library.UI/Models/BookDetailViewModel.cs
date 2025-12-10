namespace Library.UI.Models
{
    public class BookDetailViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? AuthorName { get; set; }
        public string? PublisherName { get; set; } // Entity'den eklendi
        public string? CategoryName { get; set; }
        public int? PublishYear { get; set; }
        public string? CoverImageUrl { get; set; }

        // --- GÜNCELLEME BURADA ---
        public int Status { get; set; }
        public bool IsAvailable => Status == 1; // 1 = Available

        // İstatistikler (API'den geliyorsa)
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }

        // İlişkili Veriler
        public List<ReviewViewModel> Reviews { get; set; } = new List<ReviewViewModel>();

        // Yorum Ekleme Alanları
        public int NewRating { get; set; }
        public string NewComment { get; set; } = string.Empty;
    }

    public class ReviewViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty; // User.Name + Surname birleşimi gerekebilir
        public int UserId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime Date { get; set; } // Entity adı CreatedAt
    }
}