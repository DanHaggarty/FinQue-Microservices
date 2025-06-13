using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Messaging;
using Shared.Models;
using System.Transactions;

namespace EnrichmentService.Services;

public class EnrichmentWorker : BackgroundService
{
    private readonly ILogger<EnrichmentWorker> _logger;
    private readonly ServiceBusProcessor _processor;
    private readonly ServiceBusSender _nextSender;
    private readonly Container _cosmos;

    public EnrichmentWorker(ServiceBusClient sbClient, CosmosClient cosmosClient, ILogger<EnrichmentWorker> logger)
    {
        _logger = logger;
        _processor = sbClient.CreateProcessor(QueueNames.EnrichmentQueue, new ServiceBusProcessorOptions());
        _nextSender = sbClient.CreateSender(QueueNames.RoutingQueue);
        _cosmos = cosmosClient.GetContainer("FinQueDb", "Transactions");
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor.ProcessMessageAsync += HandleMessageAsync;
        _processor.ProcessErrorAsync += args =>
        {
            _logger.LogError(args.Exception, "Error processing enrichment message");
            return Task.CompletedTask;
        };

        return _processor.StartProcessingAsync(stoppingToken);
    }

    private async Task HandleMessageAsync(ProcessMessageEventArgs args)
    {
        var id = args.Message.Body.ToString();
        _logger.LogInformation($"Enriching transaction ID: {id}");

        try
        {
            var response = await _cosmos.ReadItemAsync<Shared.Models.Transaction>(id, new PartitionKey(id));
            var tx = response.Resource;

            // Enrichment logic
            tx.RiskScore = new Random().Next(1, 100);
            tx.Tags = new List<string> { "enriched", tx.Amount > 10000 ? "high-value" : "standard" };

            await _cosmos.ReplaceItemAsync(tx, tx.Id, new PartitionKey(tx.Id));
            await _nextSender.SendMessageAsync(new ServiceBusMessage(tx.Id));

            _logger.LogInformation($"Enriched and forwarded transaction {tx.Id}");
            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to enrich transaction {id}");
            await args.AbandonMessageAsync(args.Message);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _processor.StopProcessingAsync(cancellationToken);
        await _processor.DisposeAsync();
    }
}
