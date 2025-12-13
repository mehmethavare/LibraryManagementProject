namespace Library.API.Dtos.BookDtos
{
    public class BookCreateDto
    {
        public string Title { get; set; } = null!;
        public string? AuthorName { get; set; }
        public string? PublisherName { get; set; }
        public string? CategoryName { get; set; }
        public int? PublishYear { get; set; }
        public string? CoverImageUrl { get; set; }
        public string? Location { get; set; }

    }
}
