namespace Library.API.Entities
{
    public class BorrowRecord
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public int BookId { get; set; }

        public DateTime BorrowDate { get; set; } = DateTime.Now;
        public DateTime? ReturnDate { get; set; }
        public bool IsReturned { get; set; } = false;

        // Navigation Properties
        public User? User { get; set; }
        public Book? Book { get; set; }
    }
}
