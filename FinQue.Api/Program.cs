using Azure.Messaging.ServiceBus;
using FinQue.Api.Services;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

// Extract configuration values
var serviceBusConnectionString = builder.Configuration["ServiceBus:ConnectionString"];
var queueName = builder.Configuration["ServiceBus:QueueName"];
var cosmosConnectionString = builder.Configuration["Cosmos:ConnectionString"];

// Register Service Bus Client and Sender
builder.Services.AddSingleton(new ServiceBusClient(serviceBusConnectionString));
builder.Services.AddSingleton<ServiceBusSender>(sp =>
{
    var client = sp.GetRequiredService<ServiceBusClient>();
    return client.CreateSender(queueName);
});

// Register Cosmos DB Client
builder.Services.AddSingleton(new CosmosClient(cosmosConnectionString));

// Register custom services
builder.Services.AddSingleton<ServiceBusPublisher>();

// MVC and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
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
