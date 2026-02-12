using FactorySnap.Agent;
using FactorySnap.Agent.Services;
using FactorySnap.Shared.Data;
using Opc.Ua.Client;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<FactoryContext>("FactorySnapDb");
builder.AddRedisClient("redis");

builder.Services.AddSingleton<ISessionFactory, DefaultSessionFactory>();
builder.Services.AddSingleton<OpcUaClient>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
