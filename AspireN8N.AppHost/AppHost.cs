var builder = DistributedApplication.CreateBuilder(args);


builder.AddContainer("n8n", "n8nio/n8n")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithVolume("n8n_data", "/home/node/.n8n")
    .WithHttpEndpoint(5678, 5678, "n8n");

builder.Build().Run();
