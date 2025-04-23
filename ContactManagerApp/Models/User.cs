using System;

namespace ContactManagerApp.Models
{
    public class User
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Salt { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public string UserId { get; set; }
        public string Language { get; set; } = "uk";
        public string Theme { get; set; } = "Light";
        public string Token { get; set; }
        public DateTime? TokenExpiration { get; set; }
        public string ContactFolderCode { get; set; }

        public User()
        {
            UserId = Guid.NewGuid().ToString();
            ContactFolderCode = Guid.NewGuid().ToString("N");
        }
    }
}