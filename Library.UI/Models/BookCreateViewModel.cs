using System.ComponentModel.DataAnnotations;

namespace Library.UI.Models
{
    public class BookCreateViewModel
    {
        [Display(Name = "Kitap Adı")]
        public string KitapAdi { get; set; } = string.Empty;

        [Display(Name = "Yazar Adı")]
        public string? YazarAdi { get; set; }

        [Display(Name = "Kategori")]
        public string? Kategori { get; set; }

        [Display(Name = "Yayın Yılı")]
        [Range(1000, 3000, ErrorMessage = "Yayın yılı {1} ile {2} arasında olmalıdır.")] // 👈 Örnek doğrulama
        public int? YayinYili { get; set; }

        [Display(Name = "Yayıncı Adı")]
        public string? YayinciAdi { get; set; }

        [Display(Name = "Raf / Konum")]
        public string? Location { get; set; }
        [Display(Name = "Kapak Görseli URL")]
        public string? CoverImageUrl { get; set; }
    }
}
