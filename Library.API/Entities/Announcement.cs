namespace Library.API.Entities
{
    public class Announcement
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Bu duyuruyu hangi admin oluşturdu?
        public int CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }
    }
}
