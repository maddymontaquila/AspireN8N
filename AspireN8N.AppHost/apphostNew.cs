using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithVolume("postgres_storage", "/var/lib/postgresql/data")
    .WithEnvironment("POSTGRES_USER", "n8n")
    .WithEnvironment("POSTGRES_PASSWORD", "n8n")
    .WithEnvironment("POSTGRES_DB", "n8n");

var postgresDb = postgres.AddDatabase("n8n");

// Add Qdrant vector database
var qdrant = builder.AddContainer("qdrant", "qdrant/qdrant")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHttpEndpoint(6333, 6333, "qdrant")
    .WithVolume("qdrant_storage", "/qdrant/storage");

// Add Ollama (CPU version by default)
var ollama = builder.AddContainer("ollama", "ollama/ollama", "latest")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHttpEndpoint(11434, 11434, "ollama")
    .WithVolume("ollama_storage", "/root/.ollama");

// Add Ollama model initialization container
var ollamaInit = builder.AddContainer("ollama-pull-llama", "ollama/ollama", "latest")
    .WithEntrypoint("/bin/sh")
    .WithArgs("-c", "sleep 3; ollama pull llama3.2")
    .WithEnvironment("OLLAMA_HOST", "ollama:11434")
    .WithVolume("ollama_storage", "/root/.ollama")
    .WaitFor(ollama);

// Add N8N import container for demo data
var n8nImport = builder.AddContainer("n8n-import", "n8nio/n8n", "latest")
    .WithEntrypoint("/bin/sh")
    .WithArgs("-c", "n8n import:credentials --separate --input=/demo-data/credentials && n8n import:workflow --separate --input=/demo-data/workflows")
    .WithEnvironment("DB_TYPE", "postgresdb")
    .WithEnvironment("DB_POSTGRESDB_HOST", "postgres")
    .WithEnvironment("DB_POSTGRESDB_USER", "n8n")
    .WithEnvironment("DB_POSTGRESDB_PASSWORD", "n8n")
    .WithEnvironment("N8N_DIAGNOSTICS_ENABLED", "false")
    .WithEnvironment("N8N_PERSONALIZATION_ENABLED", "false")
    .WithEnvironment("OLLAMA_HOST", "ollama:11434")
    .WithBindMount("./n8n/demo-data", "/demo-data")
    .WaitFor(postgresDb);

// Add main N8N application
var n8n = builder.AddContainer("n8n", "n8nio/n8n", "latest")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHttpEndpoint(5678, 5678, "n8n")
    .WithEnvironment("DB_TYPE", "postgresdb")
    .WithEnvironment("DB_POSTGRESDB_HOST", "postgres")
    .WithEnvironment("DB_POSTGRESDB_USER", "n8n")
    .WithEnvironment("DB_POSTGRESDB_PASSWORD", "n8n")
    .WithEnvironment("N8N_DIAGNOSTICS_ENABLED", "false")
    .WithEnvironment("N8N_PERSONALIZATION_ENABLED", "false")
    .WithEnvironment("OLLAMA_HOST", "ollama:11434")
    .WithVolume("n8n_storage", "/home/node/.n8n")
    .WithBindMount("./n8n/demo-data", "/demo-data")
    .WithBindMount("./shared", "/data/shared")
    .WaitFor(postgresDb)
    .WaitFor(n8nImport);

builder.Build().Run();