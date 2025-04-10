using System.Text;
using System.Text.Json;
using image_service.Entities;
using image_service.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace image_service.Controllers;

public class RabbitMqListener : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private IConnection? _connection;
    private IChannel? _channel;
    
    public RabbitMqListener(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Create connection and channel
            var factory = new ConnectionFactory
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest"
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();
            
            // Declare queue
            await _channel.QueueDeclareAsync(queue: "image_service",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            
            // Init consumer
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var routingKey = ea.RoutingKey;
                
                // Process the message 
                var response = await ProcessMessage(routingKey, message);
                
                // Send a response
                await _channel.BasicPublishAsync(
                    exchange: "gateway_exchange",
                    routingKey: "response." + routingKey,
                    body: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response))
                );
                
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            };
            
            // Listen to queue
            await _channel.BasicConsumeAsync(queue: "image_service",
                autoAck: false,
                consumer: consumer);

            await Task.Delay(-1, stoppingToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RabbitMQ error: {ex.Message}");
        }
    }

    private async Task<object> ProcessMessage(string routingKey, string message)
    {
        using var scope = _scopeFactory.CreateScope();
        var imageService = scope.ServiceProvider.GetRequiredService<ImageProcessingService>();
        
        object? response = null;
        
        switch (routingKey)
        {
            case "image.process":
                var imageProcessMsg = JsonSerializer.Deserialize<ImageRequest>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (imageProcessMsg is null || imageProcessMsg.ImageData == null)
                {
                    response = new { success = false, message = "Image processing request was null." };
                    break;
                }

                byte[] imageBytes = Convert.FromBase64String(imageProcessMsg.ImageData);
                string fileName = Guid.NewGuid() + ".jpg";
                
                string resp = await imageService.ProcessImage(imageBytes, fileName);
                
                response = new { success = (resp != String.Empty), ImageUrl = resp, PostId = imageProcessMsg.PostId};
                break;
        }

        return response ?? new { success = false, message = "Unhandled request." };
    }
    
    public override void Dispose()
    {
        _channel?.CloseAsync();
        _connection?.CloseAsync();
        base.Dispose();
    }
}