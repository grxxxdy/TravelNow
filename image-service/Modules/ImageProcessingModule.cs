using image_service.Controllers;
using image_service.Services;

namespace image_service.Modules;

public static class ImageProcessingModule
{
    public static void AddImageProcessingModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ImageProcessingService>();
        
        services.AddHostedService<RabbitMqListener>();
        
        services.AddControllers();
    }
}