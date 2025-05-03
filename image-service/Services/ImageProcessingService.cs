using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace image_service.Services;

public class ImageProcessingService
{
    private readonly IWebHostEnvironment _env;

    public ImageProcessingService(IWebHostEnvironment env)
    {
        _env = env;
    }
    public async Task<string> ProcessImage(byte[] imageBytes, string fileName)
    {
        var image = Image.Load(imageBytes);
        
        
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(800, 800)
        }));
        
        // SAVE IMAGE
        var imagesDir = Path.Combine(_env.ContentRootPath, "wwwroot", "images");

        if (!Directory.Exists(imagesDir))
            Directory.CreateDirectory(imagesDir);

        var savePath = Path.Combine(imagesDir, fileName);
        
        await image.SaveAsync(savePath);
        
        return $"http://localhost:5149/images/{fileName}";
    }
}