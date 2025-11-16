namespace Library.API.Dtos.UserDtos
{
    public class UserCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }

        public string Password { get; set; } = string.Empty;

        // "Admin" veya "User"
        public string Role { get; set; } = "User";
    }
}
