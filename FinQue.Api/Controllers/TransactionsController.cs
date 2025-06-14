using Azure.Messaging.ServiceBus;
using FinQue.Api.Models;
using FinQue.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Shared.Models;
using System.Text.Json;

namespace FinQue.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly ServiceBusPublisher _publisher;
        private readonly ServiceBusSender _busSender;
        private readonly Container _cosmosContainer;

        public TransactionsController(ServiceBusPublisher publisher, CosmosClient cosmosClient)
        {
            _publisher = publisher;
            _cosmosContainer = cosmosClient.GetContainer("finque-cosmos", "Transactions");
        }

        [HttpPost]
        public async Task PublishTransactionAsync(TransactionRequest request)
        {
            var transaction = new Transaction
            {
                id = Guid.NewGuid().ToString(),
                Amount = request.Amount,
                Currency = request.Currency,
                RiskScore = request.RiskScore,
                Tags = new List<string>()
            };

            // 1. Save to Cosmos
            await _cosmosContainer.CreateItemAsync(transaction, new PartitionKey(transaction.id));

            // 2. Send ID to Service Bus
            var message = new ServiceBusMessage(transaction.id);
            await _busSender.SendMessageAsync(message);
        }
    }
}
