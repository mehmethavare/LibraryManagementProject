using System.ComponentModel.DataAnnotations;

namespace Library.UI.Models
{
    public class UserCreateViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalı.")]
        public string Password { get; set; }

        // UI tarafı User oluştururken rol seçebilmeli (Admin sadece)
        public string Role { get; set; } = "User";
    }
}
