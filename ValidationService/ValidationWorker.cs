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
    private readonly TransactionValidator _transactionValidator;
    private readonly RiskValidator _riskValidator;
    private readonly ServiceBusSender _deadLetterSender;
    private readonly ServiceBusSender _highRiskSender;
    private readonly ServiceBusSender _validatedSender;

    public ValidationWorker(
        ILogger<ValidationWorker> logger, 
        ServiceBusClient serviceBusClient, 
        CosmosClient cosmosClient, 
        TransactionValidator transactionValidator, 
        RiskValidator riskValidator)
    {
        _logger = logger;
        _deadLetterSender = serviceBusClient.CreateSender("transactions-inbound/$DeadLetterQueue");
        _highRiskSender = serviceBusClient.CreateSender("transactions-highrisk");
        _validatedSender = serviceBusClient.CreateSender("transactions-validated");
        _cosmosContainer = cosmosClient.GetContainer("finque-cosmos", "Transactions");
        _transactionValidator = transactionValidator;
        _riskValidator = riskValidator;

        _processor = serviceBusClient.CreateProcessor("transactions-inbound", new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false
        });
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _processor.ProcessMessageAsync += HandleMessageAsync;
        _processor.ProcessErrorAsync += HandleErrorAsync;
        await _processor.StartProcessingAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.CompletedTask;

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

            // Deadletters message if transaction is invalid.
            if (!await ValidateTransactionAsync(transaction, args, id)) return;
            // Adds to high risk queue if risk validation fails.
            await ValidateRiskAsync(transaction, id);

            var validatedMessage = new ServiceBusMessage(JsonSerializer.Serialize(transaction))
            {
                Subject = "ValidatedTransaction"
            };

            await _validatedSender.SendMessageAsync(validatedMessage);
            _logger.LogInformation("Transaction ID {Id} routed to transactions-validated queue after successful validation", id);

            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process transaction with ID {Id}", id);
            await args.DeadLetterMessageAsync(args.Message, "ProcessingError", ex.Message);
        }
    }

    private async Task<bool> ValidateTransactionAsync(Transaction transaction, ProcessMessageEventArgs args, string id)
    {
        if (_transactionValidator.IsValid(transaction, out var reasons)) return true;

        var deadLetterMessage = new ServiceBusMessage(JsonSerializer.Serialize(transaction))
        {
            Subject = "ValidationFailed",
            ApplicationProperties = { ["Reason"] = string.Join(",", reasons) }
        };

        var description = $"Transaction validation failed for ID {id}. Reasons: {string.Join(", ", reasons)}";
        await args.DeadLetterMessageAsync(args.Message, "ValidationFailed", description);
        _logger.LogWarning("Transaction ID {Id} sent to DeadLetterQueue: {Reasons}", id, string.Join(", ", reasons));

        return false;
    }
    private async Task ValidateRiskAsync(Transaction transaction, string id)
    {
        if (_riskValidator.IsValid(transaction, out var reasons)) return;

        var highRiskMessage = new ServiceBusMessage(JsonSerializer.Serialize(transaction))
        {
            Subject = "HighRiskTransaction",
            ApplicationProperties = { ["Reason"] = string.Join(",", reasons) }
        };

        await _highRiskSender.SendMessageAsync(highRiskMessage);
        _logger.LogWarning("Transaction ID {Id} routed to transactions-highrisk queue due to risk: {Reasons}", id, string.Join(", ", reasons));
    }
    private Task HandleErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Service Bus Error: {ErrorSource}", args.ErrorSource);
        return Task.CompletedTask;
    }
}
