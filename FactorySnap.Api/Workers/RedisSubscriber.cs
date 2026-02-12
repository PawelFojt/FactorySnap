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
        
        var queue = await subscriber.SubscribeAsync(RedisChannel.Literal("live/*"));

        queue.OnMessage(async channelMessage => 
        {
            try 
            {
                var json = channelMessage.Message;
                var topic = channelMessage.Channel;

                await hubContext.Clients.All.SendAsync("ReceiveMeasure", topic, json, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Błąd przekazywania z Redis do SignalR");
            }
        });

        await Task.Delay(-1, cancellationToken);
    }
}
