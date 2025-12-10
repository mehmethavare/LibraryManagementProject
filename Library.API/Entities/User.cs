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

        // 🔹 Rol: "Admin" veya "User"
        public string Role { get; set; } = "User";  // default: normal kullanıcı

        // Navigation Properties
        public ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();
        public ICollection<BookReview> BookReviews { get; set; } = new List<BookReview>();
        public ICollection<UserRequest> Requests { get; set; } = new List<UserRequest>();
        public ICollection<Announcement> CreatedAnnouncements { get; set; } = new List<Announcement>();

    }
}
