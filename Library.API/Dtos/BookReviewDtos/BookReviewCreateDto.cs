namespace Library.API.Dtos.BookReviewDtos
{
    public class BookReviewCreateDto
    {
        public int BookId { get; set; }
        public int UserId { get; set; }
        public string Comment { get; set; } = null!;
        public int Rating { get; set; }
    }
}
