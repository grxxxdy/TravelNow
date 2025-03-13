namespace post_service.Entities;

public class Comment
{
    public int id { get; set; }
    public int post_id { get; set; }
    public int user_id { get; set; }
    public string text { get; set; }
    public DateTime created_at { get; set; } = DateTime.UtcNow;
}