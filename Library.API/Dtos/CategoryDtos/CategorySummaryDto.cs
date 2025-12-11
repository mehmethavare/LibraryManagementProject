namespace Library.API.Dtos.CategoryDtos
{
    public class CategorySummaryDto
    {
        public string CategoryName { get; set; } = string.Empty;  // Roman, Macera, Diğer
        public int TotalBooks { get; set; }                       // Bu kategorideki toplam kitap sayısı
        public int AvailableBooks { get; set; }                   // Müsait (Available) kitap sayısı
    }
}
