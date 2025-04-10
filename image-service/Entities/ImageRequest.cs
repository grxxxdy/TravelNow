namespace image_service.Entities;

public class ImageRequest
{
    public long PostId { get; set; }
    public string ImageData { get; set; } = null!;
}