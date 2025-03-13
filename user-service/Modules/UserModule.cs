using user_service.Controllers;
using user_service.Services;

namespace user_service.Modules;

public static class UserModule
{
    public static void AddUserModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<UserService>();
        
        services.AddHostedService<RabbitMqListener>();
        
        services.AddControllers();
    }
}