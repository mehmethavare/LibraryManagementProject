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
    // BookListViewModel.cs dosyanıza eklenecek sınıflar:

    // BookListViewModel.cs dosyasındaki CategoryViewModel sınıfı
    // BookListViewModel.cs dosyasındaki CategoryViewModel sınıfı

    // BookListViewModel.cs dosyasındaki CategoryViewModel sınıfı

    public class CategoryViewModel
    {
        // API'den gelen CategorySummaryDto'ya uyması için CategoryName olarak değiştirildi.
        public string CategoryName { get; set; } = string.Empty;

        // TotalBooks ve AvailableBooks'a UI'da ihtiyacımız olmasa da, 
        // deserialization'ın doğru çalışması için burada bırakmak daha güvenlidir.
        public int TotalBooks { get; set; }
        public int AvailableBooks { get; set; }

        // View'de kullanılacak adı doğrudan CategoryName'den çekiyoruz.
        public string DisplayName => CategoryName;
    }
    // BookIndexViewModel: Hem kitap listesini hem de kategorileri View'e taşımak için
    public class BookIndexViewModel
    {
        // Kitap Listesi
        public List<BookListViewModel> Books { get; set; } = new List<BookListViewModel>();

        // Filtreleme İçin Kategori Listesi
        public List<CategoryViewModel> Categories { get; set; } = new List<CategoryViewModel>();

        // Kullanıcının Seçtiği Filtre
        public string? SelectedCategory { get; set; }
    }
}