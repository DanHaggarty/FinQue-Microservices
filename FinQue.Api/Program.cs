using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;
using FinQue.Api.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Extract configuration values
var serviceBusConnectionString = builder.Configuration["ServiceBus:ConnectionString"];
var queueName = builder.Configuration["ServiceBus:QueueName"];
var cosmosConnectionString = builder.Configuration["Cosmos:ConnectionString"];

// Register Azure dependencies
builder.Services.AddSingleton(new ServiceBusClient(serviceBusConnectionString));
builder.Services.AddSingleton<ServiceBusSender>(sp =>
{
    var client = sp.GetRequiredService<ServiceBusClient>();
    return client.CreateSender(queueName);
});
builder.Services.AddSingleton(new CosmosClient(cosmosConnectionString));
builder.Services.AddSingleton<ServiceBusPublisher>();
builder.Services.AddSingleton(sp =>
{
    var vaultUri = new Uri("https://FinQueKeyVault.vault.azure.net/");
    return new SecretClient(vaultUri, new DefaultAzureCredential());
});

// Register custom services
builder.Services.AddSingleton<ISecretProvider, SecretProvider>();

// MVC
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "FinQue API", Version = "v1" });

    c.AddSecurityDefinition("AdminToken", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "X-Admin-Token",
        Type = SecuritySchemeType.ApiKey,
        Description = "Admin token required for privileged endpoints"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "AdminToken" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddSwaggerGen();

if (builder.Environment.IsProduction())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(80); // Only bind to port 80 in production (Azure)
    });

    builder.WebHost.UseUrls("http://0.0.0.0:80");
}

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
