using Library.API.Entities;

namespace Library.API.Dtos.RequestDtos
{
    public class RequestListDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public RequestStatus Status { get; set; }  // Pending / Resolved / Rejected

        public DateTime CreatedAt { get; set; }
        public string? AdminResponse { get; set; }
        public string Message { get; set; }

        // Admin tarafı listeler için
        public string? UserName { get; set; }
    }
}
