using FactorySnap.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace FactorySnap.Api.Workers;

public class RedisSubscriber(
    IConnectionMultiplexer redis,
    IHubContext<LiveDataHub> hubContext,
    ILogger<RedisSubscriber> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var subscriber = redis.GetSubscriber();
        var channel = RedisChannel.Pattern("live/*");

        await subscriber.SubscribeAsync(channel, (redisChannel, redisValue) =>
        {
            _ = PushToSignalR(redisChannel, redisValue, cancellationToken);
        });

        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    private async Task PushToSignalR(RedisChannel redisChannel, RedisValue redisValue, CancellationToken ct)
    {
        try
        {
            var topic = redisChannel.ToString();
            var json = redisValue.ToString();

            await hubContext.Clients.All.SendAsync("ReceiveMeasure", topic, json, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Błąd podczas przesyłania danych z Redis do SignalR");
        }
    }
}