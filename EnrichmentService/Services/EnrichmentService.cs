using Microsoft.Azure.Cosmos;
using Azure.Messaging.ServiceBus;
using EnrichmentService;

public partial class Program
{
    public static void Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddSingleton(new CosmosClient(builder.Configuration["Cosmos:ConnectionString"]));
        builder.Services.AddSingleton(new ServiceBusClient(builder.Configuration["ServiceBus:ConnectionString"]));
        builder.Services.AddHostedService<EnrichmentWorker>();

        var host = builder.Build();
        host.Run();
    }
}