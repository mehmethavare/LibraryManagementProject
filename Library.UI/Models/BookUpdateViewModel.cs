// Library.UI.Models/BookUpdateViewModel.cs

using System.ComponentModel.DataAnnotations; // 👈 Display niteliği için bu gerekli!

namespace Library.UI.Models
{
	public class BookUpdateViewModel
	{
		public int Id { get; set; }

		[Required(ErrorMessage = "Kitap adı boş bırakılamaz.")]
		[Display(Name = "Kitap Adı")]
		public string Title { get; set; } = string.Empty;

		[Display(Name = "Yazar Adı")]
		public string AuthorName { get; set; } = string.Empty;

		[Display(Name = "Yayıncı Adı")] 
		public string? PublisherName { get; set; } 

        [Display(Name = "Kategori")]
		public string CategoryName { get; set; } = string.Empty;

  

        [Display(Name = "Yayın Yılı")]
		[Range(1000, 3000, ErrorMessage = "Yayın yılı {1} ile {2} arasında olmalıdır.")]
		public int? PublishYear { get; set; }

		// Durum (Status) alanı sadece bilgi amaçlı kullanılabilir, düzenlenmesi genellikle API'de yapılır.
		[Display(Name = "Mevcut Durum")]
		public int Status { get; set; }

		[Display(Name = "Kapak Görseli URL")]
		public string? CoverImageUrl { get; set; }
        [Display(Name = "Raf / Konum")]
        public string? Location { get; set; }
    }
}