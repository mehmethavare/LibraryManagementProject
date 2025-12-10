namespace Library.API.Dtos.UserDtos
{
    public class UserListDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }  // EKLENDİ
        public string? ProfileImageUrl { get; set; }

        public string Role { get; set; } = "User";
    }
}
