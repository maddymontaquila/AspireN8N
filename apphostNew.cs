#:sdk Microsoft.NET.Sdk
#:sdk Aspire.AppHost.Sdk@9.4.0-preview.1.25357.1

#region imports
#:package Aspire.Hosting.AppHost@9.4.0-*
#:package Aspire.Hosting.PostgreSQL@9.4.0-*
#:package CommunityToolkit.Aspire.Hosting.Ollama@9.6.0-*
#:package Aspire.Hosting.Qdrant@9.4.0-*

#:property PublishAot=false

#endregion

#region config
Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");
Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://localhost:5003");
Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
Environment.SetEnvironmentVariable("ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL", "http://localhost:4317");
Environment.SetEnvironmentVariable("ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL", "http://localhost:5001");

Environment.SetEnvironmentVariable("Logging__LogLevel__Default", "Information");
Environment.SetEnvironmentVariable("Logging__LogLevel__Aspire.Hosting.Dcp", "Warning");
Environment.SetEnvironmentVariable("Logging__LogLevel__Microsoft.AspNetCore", "Warning");
#endregion 

var builder = DistributedApplication.CreateBuilder(args);

// Add Qdrant vector database
var qdrant = builder.AddContainer("qdrant", "qdrant/qdrant")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHttpEndpoint(6333, 6333, "qdrant")
    .WithVolume("qdrant_storage", "/qdrant/storage");

// Add Ollama (CPU version by default)
var ollama = builder.AddOllama("ollama")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithVolume("ollama_storage", "/root/.ollama")
    .WithContainerRuntimeArgs("--gpus=all");

ollama.AddModel("smollm2:135m");

// Add main N8N application
var n8n = builder.AddContainer("n8n", "n8nio/n8n", "latest")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHttpEndpoint(5678, 5678, "n8n")
    .WithEnvironment("N8N_PERSONALIZATION_ENABLED", "false")
    .WithEnvironment("OLLAMA_HOST", ollama.GetEndpoint("http"))
    .WithVolume("n8n_storage", "/home/node/.n8n")
    .WithBindMount("./shared", "/data/shared");

builder.Build().Run();