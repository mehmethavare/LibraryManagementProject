namespace Library.API.Dtos.BookReviewDtos
{
    public class BookReviewListDto
    {
        public int Id { get; set; }
        public string BookTitle { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Comment { get; set; } = null!;
        public int Rating { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
