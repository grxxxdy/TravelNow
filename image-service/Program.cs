using image_service.Modules;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

//____________________________
builder.Services.AddImageProcessingModule(builder.Configuration);
//____________________________

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();