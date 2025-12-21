using Newtonsoft.Json; // 🚨 Bu satırı en üste ekleyin

namespace Library.UI.Models
{
    public class UserListViewModel
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("surname")]
        public string? Surname { get; set; }

        [JsonProperty("email")]
        public string? Email { get; set; }

        [JsonProperty("phoneNumber")]
        public string? PhoneNumber { get; set; }

        [JsonProperty("role")]
        public string? Role { get; set; }

        // KRİTİK EKLENTİLER: API'den büyük/küçük harf duyarlılığı olmadan çekmek için
        [JsonProperty("isLocked")]
        public bool IsLocked { get; set; }

        [JsonProperty("isDeleted")]
        public bool IsDeleted { get; set; }
    }
}