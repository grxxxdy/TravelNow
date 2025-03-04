namespace user_service.Entities;

public class User
{
    public int id { get; set; }
    public string name { get; set; }
    public string email { get; set; }
    public string password { get; set; }
    
    public string role { get; set; } = "user";
    public DateTime created_at { get; set; } = DateTime.UtcNow;
}