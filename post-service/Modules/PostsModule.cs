using post_service.Controllers;
using post_service.Services;

namespace post_service.Modules;

public static class PostsModule
{
    public static void AddPostsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<PostsService>();
        
        services.AddHostedService<RabbitMqListener>();
        
        services.AddControllers();
    }
}