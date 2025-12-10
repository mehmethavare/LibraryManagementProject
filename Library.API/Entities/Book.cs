namespace Library.API.Entities
{
    public class Book
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;
        public string? AuthorName { get; set; }
        public string? PublisherName { get; set; }
        public string? CategoryName { get; set; }
        public int? PublishYear { get; set; }
        public string? CoverImageUrl { get; set; }
        public BookStatus Status { get; set; } = BookStatus.Available;
        public DateTime? ReturnedAt { get; set; }
     
        public ICollection<BorrowRecord>? BorrowRecords { get; set; }
        public ICollection<BookReview>? BookReviews { get; set; }
    }

    public enum BookStatus
    {
        Unavailable = 0,
        Available = 1
    }
}
