namespace Library.API.Dtos.UserDtos
{
    public class UserWarningsDto
    {
        public int WarningCount { get; set; }
        public bool IsLocked { get; set; }
        public bool IsDeleted { get; set; }
        public bool ShouldLogout { get; set; }
        public string? StatusMessage { get; set; }
    }
}
