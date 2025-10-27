namespace Library.API.Dtos.UserDtos
{
    public class UserListDto
    {
        public int Id { get; set; }
        public string FullName => $"{Name} {Surname}";
        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;
        public string Email { get; set; } = null!;
    }
}
