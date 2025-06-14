using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using ValidationService;
using ValidationService.Validators;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<ValidationWorker>();

// Extract config values
var serviceBusConnectionString = builder.Configuration["ServiceBus:ConnectionString"];
var queueName = builder.Configuration["ServiceBus:QueueName"];
var cosmosConnectionString = builder.Configuration["Cosmos:ConnectionString"];

// Register Service Bus Client & Sender
builder.Services.AddSingleton(new ServiceBusClient(serviceBusConnectionString));
builder.Services.AddSingleton<IMessageValidator, TransactionValidator>();
builder.Services.AddSingleton<ServiceBusSender>(sp =>
{
    var client = sp.GetRequiredService<ServiceBusClient>();
    return client.CreateSender(queueName);
});

// Register Cosmos DB Client
builder.Services.AddSingleton(new CosmosClient(cosmosConnectionString));

var host = builder.Build();
host.Run();
