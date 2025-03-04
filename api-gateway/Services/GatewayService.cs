using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace api_gateway.Services;

public class GatewayService : IDisposable
{
    private IConnection? _connection;
    private IChannel? _channel; 
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _responseTasks = new();

    public GatewayService()
    {
        Task.Run(InitializeRabbitMq).Wait();
    }

    private async Task InitializeRabbitMq()
    {
        // Create connection + channel
        var factory = new ConnectionFactory { HostName = "localhost" };
        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();
        
        // Declare exchange
        await _channel.ExchangeDeclareAsync(
            exchange: "gateway_exchange",
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false
            );
        
        // Declare user queue
        await _channel.QueueDeclareAsync(
            queue: "user_service",
            durable: true,
            exclusive: false,
            autoDelete: false
            );
        
        await _channel.QueueBindAsync(queue: "user_service", exchange: "gateway_exchange", routingKey: "user.register");
        await _channel.QueueBindAsync(queue: "user_service", exchange: "gateway_exchange", routingKey: "user.login");
        await _channel.QueueBindAsync(queue: "user_service", exchange: "gateway_exchange", routingKey: "user.get_all");
        await _channel.QueueBindAsync(queue: "user_service", exchange: "gateway_exchange", routingKey: "user.get_by_id");
        await _channel.QueueBindAsync(queue: "user_service", exchange: "gateway_exchange", routingKey: "user.update");
        await _channel.QueueBindAsync(queue: "user_service", exchange: "gateway_exchange", routingKey: "user.delete");
        
        // Declare user response queue
        await _channel.QueueDeclareAsync(
            queue: "user_responses",
            durable: true,
            exclusive: false,
            autoDelete: false
        );
        
        await _channel.QueueBindAsync(queue: "user_responses", exchange: "gateway_exchange", routingKey: "response.user.register");
        await _channel.QueueBindAsync(queue: "user_responses", exchange: "gateway_exchange", routingKey: "response.user.login");
        await _channel.QueueBindAsync(queue: "user_responses", exchange: "gateway_exchange", routingKey: "response.user.get_all");
        await _channel.QueueBindAsync(queue: "user_responses", exchange: "gateway_exchange", routingKey: "response.user.get_by_id");
        await _channel.QueueBindAsync(queue: "user_responses", exchange: "gateway_exchange", routingKey: "response.user.update");
        await _channel.QueueBindAsync(queue: "user_responses", exchange: "gateway_exchange", routingKey: "response.user.delete");
        
        // Initialize consumer for responses
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var responseMessage = Encoding.UTF8.GetString(ea.Body.ToArray());
            var routingKey = ea.RoutingKey;

            if (_responseTasks.TryRemove(routingKey, out var tcs))
            {
                tcs.SetResult(responseMessage);
            }

            await _channel.BasicAckAsync(ea.DeliveryTag, false);
        };

        await _channel.BasicConsumeAsync("user_responses", false, consumer);
    }

    public async Task<string> SendMessageAsync(string routingKey, object? message)
    {
        if (_channel == null)
        {
            return string.Empty;
        }
        
        var tcs = new TaskCompletionSource<string>();
        _responseTasks.TryAdd("response." + routingKey, tcs);
        
        // Send message
        var jsonMsg = JsonSerializer.Serialize(message);

        await _channel.BasicPublishAsync(
            exchange: "gateway_exchange",
            routingKey: routingKey,
            body: Encoding.UTF8.GetBytes(jsonMsg)
            );
        
        //Wait for response
        return await tcs.Task;
    }

    public void Dispose()
    {
        _connection?.CloseAsync();
        _channel?.CloseAsync();
    }
}