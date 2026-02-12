using System.Text.Json;
using FactorySnap.Agent.Services;
using FactorySnap.Shared.Contracts;
using FactorySnap.Shared.Data;
using FactorySnap.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace FactorySnap.Agent;

public class Worker(
    ILogger<Worker> logger,
    IServiceScopeFactory scopeFactory,
    IConnectionMultiplexer redis,
    OpcUaClient opcClient)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            await opcClient.InitializeAsync();

            opcClient.OnDataReceived = (measurement) =>
            {
                Task.Run(async () => 
                {
                    try 
                    {
                        await ProcessMeasurementAsync(measurement);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Błąd wewnątrz ProcessMeasurementAsync");
                    }
                }, cancellationToken);
            };

            string opcUrl;
            List<OpcNodeDto> nodes;

            using (var scope = scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<FactoryContext>();

                var server = await context.OpcServers.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
                if (server is null || string.IsNullOrWhiteSpace(server.Url))
                {
                    logger.LogWarning("Brak konfiguracji OPC w bazie. Worker kończy pracę.");
                    return;
                }

                opcUrl = server.Url;
                nodes = await context.OpcNodes.AsNoTracking()
                    .Where(n => n.IsEnabled)
                    .OrderBy(n => n.SortOrder)
                    .Select(n => 
                        new OpcNodeDto()
                        {
                            Id = n.Id, 
                            NodeId = n.NodeId, 
                            TagName = n.TagName,
                            IsEnabled = n.IsEnabled, 
                            SortOrder = n.SortOrder
                        })
                    .ToListAsync(cancellationToken);
            }

            if (nodes.Count == 0)
            {
                logger.LogWarning("Brak aktywnych nodów OPC do monitorowania.");
                return;
            }

            await opcClient.ConnectAsync(opcUrl);

            foreach (var node in nodes)
            {
                opcClient.MonitorNode(node.NodeId, node.TagName);
            }

            logger.LogInformation("Worker nasłuchuje zmian na serwerze OPC UA...");

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Krytyczny błąd Workera OPC UA. Serwis zostanie zatrzymany.");
        }
    }

    private async Task ProcessMeasurementAsync(Measurement measurement)
    {
        try
        {
            var db = redis.GetDatabase();
            var json = JsonSerializer.Serialize(measurement);
            var channel = $"live/{measurement.MachineId}/{measurement.TagName}";

            await db.PublishAsync(RedisChannel.Literal(channel), json);

            using (var scope = scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<FactoryContext>();
                context.Measurements.Add(measurement);
                await context.SaveChangesAsync();
            }

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Przetworzono: {TagName} = {Value}", measurement.TagName,
                    measurement.ValNum);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Błąd podczas przetwarzania pomiaru z OPC UA");
        }
    }
}