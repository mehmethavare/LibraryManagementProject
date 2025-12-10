using Library.API.Entities;

namespace Library.API.Dtos.RequestDtos
{
    public class RequestUpdateStatusDto
    {
        public RequestStatus Status { get; set; }  // Pending / Resolved / Rejected

        public string? AdminResponse { get; set; }
    }
}
