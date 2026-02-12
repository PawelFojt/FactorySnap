var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");

var postgres = builder.AddPostgres("timescaledb")
    .WithContainerName("FactorySnapInstance")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithImage("timescale/timescaledb-ha") 
    .WithImageTag("pg16")
    .WithDataVolume() 
    .WithPgAdmin();

var factoryDb = postgres.AddDatabase("FactorySnapDb");

var agent = builder.AddProject<Projects.FactorySnap_Agent>("agent")
    .WithReference(redis)
    .WithReference(factoryDb);

builder.AddProject<Projects.FactorySnap_AnomalyDetector>("anomaly-detector")
    .WithReference(redis);

var api = builder.AddProject<Projects.FactorySnap_Api>("api")
    .WithHttpHealthCheck("/health")
    .WithReference(redis)
    .WithReference(factoryDb);

builder.AddProject<Projects.FactorySnap_Client>("client")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(redis)
    .WaitFor(redis)
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
