using System;
using System.Text.Json.Serialization;

namespace ChatAgenda.Models
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        
        [JsonIgnore]
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "Employee"; // Admin, Supervisor, Employee
        public string Department { get; set; } = "General";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
