    using Azure.Messaging.ServiceBus;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Shared.Models;
    using System.Collections.Concurrent;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Drawing;
    using System.Text.Json;
    using ValidationService.Validators;
    using Container = Microsoft.Azure.Cosmos.Container;

    namespace ValidationService;

    public class ValidationWorker : BackgroundService
    {
        private readonly ILogger<ValidationWorker> _logger;
        private readonly ServiceBusProcessor _processor;
        private readonly Container _cosmosContainer;
        private readonly IMessageValidator _validator;
        private readonly ServiceBusSender _deadLetterSender;

    public ValidationWorker(ILogger<ValidationWorker> logger, ServiceBusClient serviceBusClient, CosmosClient cosmosClient, IMessageValidator validator)
        {
            _logger = logger;
            _deadLetterSender = serviceBusClient.CreateSender("transactions-inbound/$DeadLetterQueue");
            _processor = serviceBusClient.CreateProcessor("transactions-inbound", new ServiceBusProcessorOptions());
            _cosmosContainer = cosmosClient.GetContainer("finque-cosmos", "Transactions");
            _validator = validator;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _processor.ProcessMessageAsync += HandleMessageAsync;
            _processor.ProcessErrorAsync += HandleErrorAsync;
            await _processor.StartProcessingAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
            Task.CompletedTask; // Not needed; work is done via StartAsync and message handler.

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
        }

    private async Task HandleMessageAsync(ProcessMessageEventArgs args)
    {
        var id = args.Message.Body.ToString();

        _logger.LogInformation("Received ID: {Id}", id);

        try
        {
            var response = await _cosmosContainer.ReadItemAsync<Transaction>(id, new PartitionKey(id));
            var transaction = response.Resource;

            _logger.LogInformation("Transaction loaded: {Json}", JsonSerializer.Serialize(transaction));

            if (!_validator.IsValid(transaction, out var reasons))
            {
                var deadLetterMessage = new ServiceBusMessage(JsonSerializer.Serialize(transaction))
                {
                    Subject = "ValidationFailed",
                    ApplicationProperties = { ["Reason"] = string.Join(",", reasons) }
                };
                var description = $"Transaction validation failed for ID {id}. Reasons: {string.Join(", ", reasons)}";
                await args.DeadLetterMessageAsync(args.Message, "ValidationFailed", description);
                return;
            }

            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process transaction with ID {Id}", id);
            await args.DeadLetterMessageAsync(args.Message, "ProcessingError", ex.Message);
        }
    }

        private Task HandleErrorAsync(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception, "Service Bus Error: {ErrorSource}", args.ErrorSource);
            return Task.CompletedTask;
        }
    }
