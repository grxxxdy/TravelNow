namespace post_service.Entities;

public class Like
{
    public int id { get; set; }
    public int post_id { get; set; }
    public int user_id { get; set; }
    public DateTime created_at { get; set; } = DateTime.UtcNow;
}