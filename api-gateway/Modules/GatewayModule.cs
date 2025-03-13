using api_gateway.Controllers;
using api_gateway.Services;

namespace api_gateway.Modules;

public static class GatewayModule
{
    public static void AddGatewayModule(this IServiceCollection services)
    {
        services.AddSingleton<GatewayService>();
        services.AddControllers();
    }
}