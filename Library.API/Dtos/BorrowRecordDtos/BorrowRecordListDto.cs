namespace Library.API.Dtos.BorrowRecordDtos
{
    public class BorrowRecordListDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = null!;
        public string BookTitle { get; set; } = null!;
        public int BookId { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public bool IsReturned { get; set; }
        public string? CoverImageUrl { get; set; }
        public int ReturnRequestStatus { get; set; }
    }
}
