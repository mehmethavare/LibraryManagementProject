using Library.API.Entities;

namespace Library.API.Dtos.BookDtos
{
    public class BookListDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? AuthorName { get; set; }
        public string? CategoryName { get; set; }
        public int? PublishYear { get; set; }
        public BookStatus Status { get; set; }          // Available / Unavailable
    }
}
