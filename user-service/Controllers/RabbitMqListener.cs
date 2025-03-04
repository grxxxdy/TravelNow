using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using user_service.Entities;
using user_service.Services;

namespace user_service.Controllers;

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
            _channel.QueueDeclareAsync(queue: "user_service",
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
            await _channel.BasicConsumeAsync(queue: "user_service",
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
        var userService = scope.ServiceProvider.GetRequiredService<UserService>();

        object? response = null;
        
        switch (routingKey)
        {
            case "user.register":
                var registerMsg = JsonSerializer.Deserialize<User>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (registerMsg == null)
                {
                    response = new { success = false, message = "User could not be registered : message was null." };
                    break;
                }
                
                string resp = await userService.CreateUserAsync(registerMsg);
                response = new { success = (resp == "User created successfully."), message = resp };
                break;
            
            case "user.login":
                var loginMsg = JsonSerializer.Deserialize<Dictionary<string, string>>(message);

                if (loginMsg == null || !loginMsg.ContainsKey("email") || !loginMsg.ContainsKey("password"))
                {
                    response = new { success = false, message = "Invalid login request." };
                    break;
                }
                
                var token = await userService.LoginUser(loginMsg["email"], loginMsg["password"]);
                response = new { success = (token != "User not found." && token != "Incorrect password."), message = token };
                break;
                
            case "user.get_all":
                response = await userService.GetAllUsersAsync();
                break;

            case "user.get_by_id":
                var getUserMsg = JsonSerializer.Deserialize<User>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (getUserMsg == null || getUserMsg.id <= 0)
                {
                    Console.WriteLine($"Invalid user ID for GET: {message}");
                    response = new { success = false, message = "User could not be found: invalid request." };
                    break;
                }
                
                response = await userService.GetUserByIdAsync(getUserMsg.id);
                break;

            case "user.update":
                var updateMsg = JsonSerializer.Deserialize<User>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (updateMsg == null)
                {
                    response = new { success = false, message = "User could not be updated : message was null." };
                    break;
                }
                
                var res = await userService.UpdateUserAsync(updateMsg.id, updateMsg);
                response = new { success = (res == "User updated successfully."), message = res };
                break;

            case "user.delete":
                var deleteUserMsg = JsonSerializer.Deserialize<User>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (deleteUserMsg == null || deleteUserMsg.id <= 0)
                {
                    response = new { success = false, message = "User could not be deleted: invalid request." };
                    break;
                }
                
                bool delRes = await userService.DeleteUserAsync(deleteUserMsg.id);
                response = delRes ? new { success = true, message = "User deleted successfully." } : new { success = false, message = "User could not be deleted." };
                
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