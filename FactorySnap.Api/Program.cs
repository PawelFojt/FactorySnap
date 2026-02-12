using FactorySnap.Api.Hubs;
using FactorySnap.Api.Workers;
using FactorySnap.Shared.Contracts;
using FactorySnap.Shared.Data;
using FactorySnap.Shared.Entities;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<FactoryContext>("FactorySnapDb");
builder.AddRedisOutputCache("redis");
builder.AddRedisClient("redis");
builder.Services.AddSignalR();
builder.Services.AddHostedService<RedisSubscriber>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyMethod()
            .AllowAnyHeader()
            .SetIsOriginAllowed(_ => true) 
            .AllowCredentials(); 
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<FactoryContext>();
    await context.EnsureTimescaleEnabled();
}

app.MapDefaultEndpoints();
app.UseCors("AllowAll");
app.UseOutputCache();

app.MapHub<LiveDataHub>("/hubs/live");
app.MapGet("/api/history/{machineId}/{tagName}", 
    async (string machineId, string tagName, DateTime? from, DateTime? to, FactoryContext db) =>
{
    var start = from ?? DateTime.UtcNow.AddHours(-1);
    var end = to ?? DateTime.UtcNow;

    var data = await db.Measurements
        .AsNoTracking()
        .Where(m => m.MachineId == machineId 
                 && m.TagName == tagName 
                 && m.Timestamp >= start 
                 && m.Timestamp <= end)
        .OrderBy(m => m.Timestamp)
        .Select(m => new { t = m.Timestamp, v = m.ValNum })
        .ToListAsync();

    return Results.Ok(data);
})
.CacheOutput(x => x.Expire(TimeSpan.FromSeconds(5)).SetVaryByRouteValue("machineId", "tagName"));

app.MapGet("/api/opc/config", async (FactoryContext db) =>
{
    var server = await db.OpcServers.AsNoTracking().FirstOrDefaultAsync();
    var nodes = await db.OpcNodes.AsNoTracking()
        .OrderBy(n => n.SortOrder)
        .ToListAsync();

    var dto = new OpcConfigDto()
    {
        Url = server?.Url ?? string.Empty,
        Nodes = nodes.Select(n => new OpcNodeDto
        {
            Id = n.Id, 
            NodeId = n.NodeId, 
            TagName = n.TagName, 
            IsEnabled = n.IsEnabled, 
            SortOrder = n.SortOrder
        }).ToList()
    };

    return Results.Ok(dto);
});

app.MapPut("/api/opc/config", async (OpcConfigDto dto, FactoryContext db) =>
{
    var server = await db.OpcServers.FirstOrDefaultAsync();
    if (server is null)
    {
        server = new OpcServerConfig { Url = dto.Url, IsActive = true, UpdatedAtUtc = DateTime.UtcNow };
        db.OpcServers.Add(server);
    }
    else
    {
        server.Url = dto.Url;
        server.IsActive = true;
        server.UpdatedAtUtc = DateTime.UtcNow;
    }

    var existingNodes = await db.OpcNodes.ToListAsync();
    var incomingIds = dto.Nodes.Select(n => n.Id).ToHashSet();

    foreach (var nodeDto in dto.Nodes)
    {
        if (nodeDto.Id == 0)
        {
            db.OpcNodes.Add(new OpcNodeConfig
            {
                NodeId = nodeDto.NodeId,
                TagName = nodeDto.TagName,
                IsEnabled = nodeDto.IsEnabled,
                SortOrder = nodeDto.SortOrder
            });
            continue;
        }

        var entity = existingNodes.FirstOrDefault(x => x.Id == nodeDto.Id);
        if (entity is null) continue;

        entity.NodeId = nodeDto.NodeId;
        entity.TagName = nodeDto.TagName;
        entity.IsEnabled = nodeDto.IsEnabled;
        entity.SortOrder = nodeDto.SortOrder;
    }

    var toRemove = existingNodes.Where(x => !incomingIds.Contains(x.Id)).ToList();
    if (toRemove.Count > 0)
    {
        db.OpcNodes.RemoveRange(toRemove);
    }

    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();