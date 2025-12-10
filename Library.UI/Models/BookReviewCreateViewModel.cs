using System.ComponentModel.DataAnnotations;

namespace Library.UI.Models
{
    public class BookReviewCreateViewModel
    {
        public int BookId { get; set; }

        // HATA ÇÖZÜMÜ: Bu satır eksikti, ekledik.
        public int UserId { get; set; }

        [Range(1, 5, ErrorMessage = "Puan 1 ile 5 arasında olmalıdır.")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Yorum yazılması zorunludur.")]
        public string Comment { get; set; } = string.Empty;
    }
}