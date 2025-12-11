namespace Library.API.Entities
{
    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }

        public string Password { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }

        // "Admin" veya "User" (default: User)
        public string Role { get; set; } = "User";

        //  Uyarı ve hesap durumu
        public int WarningCount { get; set; } = 0;   // Kaç kez geç iade yaptı
        public bool IsLocked { get; set; } = false;  // 2. uyarıda true olacak
        public bool IsDeleted { get; set; } = false; // 3. uyarıda soft delete
        public ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();
        public ICollection<BookReview> BookReviews { get; set; } = new List<BookReview>();
        public ICollection<UserRequest> Requests { get; set; } = new List<UserRequest>();
        public ICollection<Announcement> CreatedAnnouncements { get; set; } = new List<Announcement>();
    }
}
