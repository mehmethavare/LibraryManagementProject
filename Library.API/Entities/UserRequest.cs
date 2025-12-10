namespace Library.API.Entities
{
    public class UserRequest
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public RequestStatus Status { get; set; } = RequestStatus.Pending;
        public string? AdminResponse { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public User? User { get; set; }
    }

    public enum RequestStatus
    {
        Pending = 0,   // Beklemede
        Resolved = 1,  // Çözüldü
        Rejected = 2   // Reddedildi
    }
}
