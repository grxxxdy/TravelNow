using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using api_gateway.Config;
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
            exchange: QueueConfig.ExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false
            );
        
        // Declare queues
        foreach (var queue in QueueConfig.Queues)
        {
            await _channel.QueueDeclareAsync(
                queue: queue.Key,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            foreach (var rKey in queue.Value)
            {
                await _channel.QueueBindAsync(queue: queue.Key, exchange: QueueConfig.ExchangeName, routingKey: rKey);
            }
        }
        
        // Initialize consumers for responses
        foreach (var queue in QueueConfig.Queues.Where(q => q.Key.EndsWith("responses")))
        {
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

            await _channel.BasicConsumeAsync(queue.Key, false, consumer);
        }
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
            exchange: QueueConfig.ExchangeName,
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