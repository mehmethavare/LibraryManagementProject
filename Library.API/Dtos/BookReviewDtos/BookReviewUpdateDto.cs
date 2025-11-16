namespace Library.API.Dtos.BookReviewDtos
{
    public class BookReviewUpdateDto
    {
        public string Comment { get; set; } = string.Empty;
        public int Rating { get; set; }  // 1–5 arası olacak, controller zaten kontrol ediyor
    }
}
