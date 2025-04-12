public class User
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string Salt { get; set; } 
    public string Role { get; set; }
    public bool IsActive { get; set; }
    public string UserId { get; set; }
}