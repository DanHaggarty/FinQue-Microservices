using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Messaging;
using Shared.Models;
using System.Collections.Concurrent;
using System.ComponentModel;
using Container = Microsoft.Azure.Cosmos.Container;

namespace RoutingService
{
    /// <summary>
    /// Processes enriched transactions from routing-queue and routes them based on risk and amount.
    /// </summary>
    public class RoutingWorker : BackgroundService
    {
        private readonly ILogger<RoutingWorker> _logger;
        private readonly ServiceBusProcessor _processor;
        private readonly ServiceBusClient _busClient;
        private readonly Container _cosmos;

        public RoutingWorker(ServiceBusClient busClient, CosmosClient cosmosClient, ILogger<RoutingWorker> logger)
        {
            _logger = logger;
            _busClient = busClient;
            _processor = busClient.CreateProcessor(QueueNames.Inbound, new ServiceBusProcessorOptions());
            _cosmos = cosmosClient.GetContainer("FinQueDb", "Transactions");
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _processor.ProcessMessageAsync += HandleMessageAsync;
            _processor.ProcessErrorAsync += args =>
            {
                _logger.LogError(args.Exception, "Error processing routing message");
                return Task.CompletedTask;
            };

            return _processor.StartProcessingAsync(stoppingToken);
        }

        private async Task HandleMessageAsync(ProcessMessageEventArgs args)
        {
            var id = args.Message.Body.ToString();
            _logger.LogInformation($"Routing transaction ID: {id}");

            try
            {
                var response = await _cosmos.ReadItemAsync<Transaction>(id, new PartitionKey(id));
                var tx = response.Resource;

                string targetQueue;

                if (tx.RiskScore > 80)
                    targetQueue = "fraud-queue";
                else if (tx.Amount > 10000)
                    targetQueue = "audit-queue";
                else
                    targetQueue = "approval-queue";

                tx.Tags.Add($"routed:{targetQueue}");
                await _cosmos.ReplaceItemAsync(tx, tx.id, new PartitionKey(tx.id));

                var sender = _busClient.CreateSender(targetQueue);
                await sender.SendMessageAsync(new ServiceBusMessage(tx.id));

                _logger.LogInformation($"Transaction {tx.id} routed to {targetQueue}");
                await args.CompleteMessageAsync(args.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Routing failed for transaction {id}");
                await args.AbandonMessageAsync(args.Message);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
        }
    }
}
