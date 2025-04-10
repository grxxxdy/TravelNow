namespace post_service.Entities;

public class Post
{
    public int id { get; set; }
    public int user_id { get; set; }
    public string text { get; set; }
    public string image_url { get; set; }
    public DateTime created_at { get; set; }
}