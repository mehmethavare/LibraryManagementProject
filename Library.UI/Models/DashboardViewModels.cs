using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Library.UI.Models
{
    // === YARDIMCI MODELLER (Dashboard'lar listeleri için) ===

    public class AnnouncementListViewModel
    {
        public int Id { get; set; }

        public string? Title { get; set; }
        public string? Content { get; set; }

        [JsonProperty("CreatedAt")]
        public DateTime Date { get; set; }

        // Admin tarafında “kim yayınladı?” göstermek için
        public string? CreatedByName { get; set; }
    }
    // Duyuru Oluşturma Modeli (Admin formundan gelen veri)
    public class CreateAnnouncementViewModel
    {
        // Başlık için kısıtlamalar düşürüldü: En az 1, En fazla 150 karakter.
        [Required(ErrorMessage = "Duyuru başlığı girmek zorunludur.")]
        [StringLength(150, MinimumLength = 1, ErrorMessage = "Başlık 1 ile 150 karakter arasında olmalıdır.")]
        public string Title { get; set; } = string.Empty;

        // İçerik için kısıtlamalar düşürüldü: En az 5, En fazla 1000 karakter.
        [Required(ErrorMessage = "Duyuru içeriği boş bırakılamaz.")]
        [StringLength(1000, MinimumLength = 5, ErrorMessage = "İçerik en az 5 karakter, en fazla 1000 karakter olmalıdır.")]
        public string Content { get; set; } = string.Empty;
    }

    // === ANA DASHBOARD MODELLERİ ===

    // 1. KULLANICI İÇİN DASHBOARD MODELİ
    public class UserDashboardViewModel
    {
        public List<AnnouncementListViewModel>? Announcements { get; set; }
        public List<RequestListViewModel>? MyRequests { get; set; }
        public List<BorrowRecordListViewModel>? ActiveBorrows { get; set; }
        public List<BookReviewListViewModel>? MyReviews { get; set; }
    }

    // 2. ADMİN İÇİN DASHBOARD MODELİ
    public class AdminDashboardViewModel
    {
        public int TotalBooks { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveBorrows { get; set; }
        public List<RequestListViewModel>? LatestRequests { get; set; }
        public List<BorrowRecordListViewModel> RecentActiveBorrows { get; set; } = new List<BorrowRecordListViewModel>();

    }
  
}