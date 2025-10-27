namespace Library.API.Dtos.BorrowRecordDtos
{
    public class BorrowRecordCreateDto
    {
        public int UserId { get; set; }
        public int BookId { get; set; }
        public DateTime BorrowDate { get; set; } = DateTime.Now;
        public DateTime? ReturnDate { get; set; }
    }
}
