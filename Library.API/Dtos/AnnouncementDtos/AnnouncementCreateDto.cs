using System.ComponentModel.DataAnnotations;

namespace Library.API.Dtos.AnnouncementDtos
{
    public class AnnouncementCreateDto
    {
 
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
