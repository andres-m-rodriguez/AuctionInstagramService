var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder
    .AddAzurePostgresFlexibleServer("postgres")
    .RunAsContainer(c => c.WithDataVolume().WithPgAdmin());

var auctionDb = postgres.AddDatabase("auctiondb");

var redis = builder.AddAzureManagedRedis("redis").RunAsContainer(c => c.WithRedisInsight());

var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var blobs = storage.AddBlobs("blobs");

var apiService = builder
    .AddProject<Projects.AuctionInstagramService_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(auctionDb).WaitFor(auctionDb)
    .WithReference(redis).WaitFor(redis)
    .WithReference(blobs).WaitFor(blobs);

var streamingService = builder
    .AddProject<Projects.AuctionInstagramService_StreamingService>("streamingservice")
    .WithHttpHealthCheck("/health")
    .WithReference(redis).WaitFor(redis);

builder
    .AddProject<Projects.AuctionInstagramService_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService).WaitFor(apiService)
    .WithReference(streamingService).WaitFor(streamingService);

builder.Build().Run();
