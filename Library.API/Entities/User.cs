namespace Library.API.Entities
{
    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }

        // Navigation Properties
        public ICollection<BorrowRecord>? BorrowRecords { get; set; }
        public ICollection<BookReview>? BookReviews { get; set; }
    }
}
