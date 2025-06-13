using EnrichmentService;
using Microsoft.Azure.Cosmos;
using Azure.Messaging.ServiceBus;


var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<EnrichmentWorker>();

var host = builder.Build();
host.Run();
