namespace Library.UI.Models
{
    public class BookReviewViewModel
    {
        public int Id { get; set; }
        public int BookId { get; set; }

        public string BookTitle { get; set; } = string.Empty;

        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;

        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}