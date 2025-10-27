namespace Library.API.Entities
{
    public class BookReview
    {
        public int Id { get; set; }

        public int BookId { get; set; }
        public int UserId { get; set; }

        public string Comment { get; set; } = null!;
        public int Rating { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public Book? Book { get; set; }
        public User? User { get; set; }
    }
}
