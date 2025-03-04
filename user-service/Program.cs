using Microsoft.EntityFrameworkCore;
using user_service.Modules;
using user_service.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


//____________________________
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services.AddUserModule(builder.Configuration);

builder.Services.AddAuthModule(builder.Configuration);
//____________________________

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();