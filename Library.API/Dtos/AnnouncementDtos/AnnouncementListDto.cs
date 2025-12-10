namespace Library.API.Dtos.AnnouncementDtos
{
    public class AnnouncementListDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        // Admin tarafında “kim yayınladı?” göstermek için
        public string? CreatedByName { get; set; }
    }
}
