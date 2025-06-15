using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;
using FinQue.Api.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// === Configuration ===
var configuration = builder.Configuration;
var serviceBusConnectionString = configuration["ServiceBus:ConnectionString"];
var queueName = configuration["ServiceBus:QueueName"];
var cosmosConnectionString = configuration["Cosmos:ConnectionString"];
var keyVaultUri = new Uri("https://FinQueKeyVault.vault.azure.net/");

if (string.IsNullOrWhiteSpace(serviceBusConnectionString) ||
    string.IsNullOrWhiteSpace(queueName) ||
    string.IsNullOrWhiteSpace(cosmosConnectionString))
{
    throw new InvalidOperationException("Required configuration is missing.");
}

// === Dependency Injection ===

// Azure clients
builder.Services.AddSingleton(new ServiceBusClient(serviceBusConnectionString));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<ServiceBusClient>().CreateSender(queueName));
builder.Services.AddSingleton(new CosmosClient(cosmosConnectionString));
builder.Services.AddSingleton(new SecretClient(keyVaultUri, new DefaultAzureCredential()));

// Custom services
builder.Services.AddSingleton<ServiceBusPublisher>();
builder.Services.AddSingleton<ISecretProvider, SecretProvider>();

// Controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FinQue API", Version = "v1" });

    c.AddSecurityDefinition("AdminToken", new OpenApiSecurityScheme
    {
        Name = "X-Admin-Token",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Description = "Admin token required for privileged endpoints"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "AdminToken"
                }
            },
            Array.Empty<string>()
        }
    });
});

// === Hosting ===
if (builder.Environment.IsProduction())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(80); // Bind to port 80 in production
    });

    builder.WebHost.UseUrls("http://0.0.0.0:80");
}

// === App pipeline ===
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FinQue API V1");
    c.RoutePrefix = string.Empty;
});

//app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();
