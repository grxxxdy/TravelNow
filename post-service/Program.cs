using Microsoft.EntityFrameworkCore;
using post_service.Modules;
using post_service.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


//____________________________
builder.Services.AddDbContext<PostsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddPostsModule(builder.Configuration);
//____________________________

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();