using FactorySnap.Shared.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<FactoryContext>("timescaledb");
builder.AddRedisOutputCache("redis");

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseOutputCache();

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
// -------------------------------------------

app.Run();