using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using ValidationService;
using ValidationService.Validators;

var builder = Host.CreateApplicationBuilder(args);

// Extract config values
var serviceBusConnectionString = builder.Configuration["ServiceBus:ConnectionString"];
var queueName = builder.Configuration["ServiceBus:QueueName"];
var cosmosConnectionString = builder.Configuration["Cosmos:ConnectionString"];

// Fail fast if config missing
if (string.IsNullOrWhiteSpace(serviceBusConnectionString) ||
    string.IsNullOrWhiteSpace(queueName) ||
    string.IsNullOrWhiteSpace(cosmosConnectionString))
{
    throw new InvalidOperationException("Required configuration is missing.");
}

// DI container registrations
builder.Services.AddSingleton(new ServiceBusClient(serviceBusConnectionString));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<ServiceBusClient>().CreateSender(queueName));

builder.Services.AddSingleton(new CosmosClient(cosmosConnectionString));
builder.Services.AddSingleton<TransactionValidator>();
builder.Services.AddSingleton<RiskValidator>();
builder.Services.AddHostedService<ValidationWorker>();

var host = builder.Build();
host.Run();
