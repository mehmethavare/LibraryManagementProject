using System;
using System.ComponentModel.DataAnnotations; // 🚨 YENİ EKLENDİ

namespace Library.UI.Models
{
    // 1. LİSTELEME İÇİN (Hem Admin hem User listesi bunu kullanır)
    public class RequestListViewModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }

        public string Title { get; set; }
        public string Message { get; set; }
        public int Status { get; set; }
        public string? AdminResponse { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    // 2. YENİ İSTEK OLUŞTURMA İÇİN (Sadece bu iki veri lazım)
    public class CreateRequestViewModel
    {
        [Required(ErrorMessage = "Kitap Adı zorunludur.")]
        [StringLength(100, ErrorMessage = "Kitap Adı en fazla 100 karakter olmalıdır.")]
        public string Title { get; set; }

        // 🚨 GÜNCELLENDİ: Hem zorunlu kılındı hem de minimum karakter uzunluğu eklendi.
        [Required(ErrorMessage = "Mesaj alanı zorunludur.")]
        [MinLength(10, ErrorMessage = "Mesajınız en az 10 kelimeyi (yaklaşık 50 karakter) içermelidir.")]
        public string Message { get; set; }
    }

    // 3. ADMİN CEVAP VERME İÇİN (Sadece bu veriler lazım)
    public class UpdateRequestStatusViewModel
    {
        public int Id { get; set; }
        public int Status { get; set; }
        public string AdminResponse { get; set; }
    }
    // RequestViewModel.cs dosyasına eklenecek yeni sınıf:
    public class EditRequestViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
    }
    // RequestViewModel.cs dosyanıza eklenecek yeni sınıf (Library.UI.Models namespace'i içinde)

    // RequestStatus zaten bu namespace'de olduğu için sorun olmayacaktır.
    public class RequestDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        // ÖNEMLİ: RequestStatus artık Library.UI.Models içinde olmalı.
        // public RequestStatus Status { get; set; } // Bu satırda RequestStatus'un tanımına ihtiyacınız olabilir.

        public string? AdminResponse { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }

        // Kullanıcının ad ve maili için eklenmişti.
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
    }
}