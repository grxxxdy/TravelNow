using System.Text;
using System.Text.Json;
using post_service.Entities;
using post_service.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace post_service.Controllers;

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
            await _channel.QueueDeclareAsync(queue: "post_service",
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
            await _channel.BasicConsumeAsync(queue: "post_service",
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
        var postsService = scope.ServiceProvider.GetRequiredService<PostsService>();

        object? response = null;
        
        switch (routingKey)
        {
            case "post.create":
                var createMsg = JsonSerializer.Deserialize<Post>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (createMsg == null)
                {
                    response = new { success = false, message = "Post could not be created : message was null." };
                    break;
                }
                
                string resp = await postsService.CreatePostAsync(createMsg);
                response = new { success = (resp == "Post created successfully."), message = resp };
                break;
                
            case "post.get_all":
                response = await postsService.GetAllPostsAsync();
                break;
            
            case "post.get_by_id":
                var getPostMsg = JsonSerializer.Deserialize<Post>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (getPostMsg == null || getPostMsg.id <= 0)
                {
                    Console.WriteLine($"Invalid post ID for GET: {message}");
                    response = new { success = false, message = "Post could not be found: invalid request." };
                    break;
                }
                
                response = await postsService.GetPostByIdAsync(getPostMsg.id);
                break;
            
            case "post.update":
                var updateMsg = JsonSerializer.Deserialize<Post>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (updateMsg == null)
                {
                    response = new { success = false, message = "Post could not be updated : message was null." };
                    break;
                }
                
                var res = await postsService.UpdatePostAsync(updateMsg.id, updateMsg);
                response = new { success = (res == "Post updated successfully."), message = res };
                break;
            
            case "post.delete":
                var deletePostMsg = JsonSerializer.Deserialize<Post>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (deletePostMsg == null || deletePostMsg.id <= 0)
                {
                    response = new { success = false, message = "Post could not be deleted: invalid request." };
                    break;
                }
                
                bool delRes = await postsService.DeletePostAsync(deletePostMsg.id);
                response = delRes ? new { success = true, message = "Post deleted successfully." } : new { success = false, message = "Post could not be deleted." };
                
                break;
            case "post.like":
                var likeMsg = JsonSerializer.Deserialize<Like>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (likeMsg == null)
                {
                    response = new { success = false, message = "Post could not be liked: invalid request." };
                    break;
                }
                
                response = await postsService.LikePostAsync(likeMsg);
                break;
            case "post.comment":
                var commentMsg = JsonSerializer.Deserialize<Comment>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (commentMsg == null)
                {
                    response = new { success = false, message = "Post could not be commented on: invalid request." };
                    break;
                }
                
                response = await postsService.CommentPostAsync(commentMsg);
                break;
            case "post.likes_get":
                var getLikesMsg = JsonSerializer.Deserialize<Post>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (getLikesMsg == null || getLikesMsg.id <= 0)
                {
                    Console.WriteLine($"Invalid post ID for likes_get: {message}");
                    response = new { success = false, message = "Post could not be found: invalid request." };
                    break;
                }
                
                response = await postsService.GetAllLikesAsync(getLikesMsg.id);
                break;
            case "post.comments_get":
                var getCommentsMsg = JsonSerializer.Deserialize<Post>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (getCommentsMsg == null || getCommentsMsg.id <= 0)
                {
                    Console.WriteLine($"Invalid post ID for comments_get: {message}");
                    response = new { success = false, message = "Post could not be found: invalid request." };
                    break;
                }
                
                response = await postsService.GetAllCommentsAsync(getCommentsMsg.id);
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