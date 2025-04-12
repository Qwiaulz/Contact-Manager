namespace ContactManagerApp.Models
{
    public class Contact
    {
        public string Id { get; set; } // Додано Id
        public string UserId { get; set; } // ID користувача, до якого належить контакт
        public string Name { get; set; }
        public string Phone { get; set; } // Замінили з PhoneNumber на Phone
        public string Email { get; set; }
    }
}