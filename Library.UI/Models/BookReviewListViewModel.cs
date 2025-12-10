namespace Library.UI.Models
{
    public class BookReviewListViewModel
    {
        public int Id { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public int Rating { get; set; }
        
        public DateTime CreatedAt { get; set; }
    }
}
