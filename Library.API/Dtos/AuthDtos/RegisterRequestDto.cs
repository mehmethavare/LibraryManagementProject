namespace Library.API.Dtos.AuthDtos
{
    public class RegisterRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }

        public string Password { get; set; } = string.Empty;
    }
}
