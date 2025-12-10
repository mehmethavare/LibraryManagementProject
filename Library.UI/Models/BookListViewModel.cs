namespace Library.UI.Models
{
    public class BookListViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? AuthorName { get; set; }

        // Entity'de CoverImage yok ama UI'da görsel kullanıyorsan burada kalabilir.
        // API göndermiyorsa boş gelir, sorun olmaz.
        public string? CoverImageUrl { get; set; }
        public string? CategoryName { get; set; }
        public string? PublisherName { get; set; }


        // --- GÜNCELLEME BURADA ---
        // API'den gelen "Status" değerini (0 veya 1) buraya alıyoruz.
        public int Status { get; set; }

        // HTML tarafında if(IsAvailable) diyebilmek için yardımcı özellik:
        // Eğer Status 1 ise (Available) -> True döner.
        public bool IsAvailable => Status == 1;
    }
}