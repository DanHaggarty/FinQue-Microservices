using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;

namespace FinQue.Api.Controllers
{
    /// <summary>
    /// Provides administrative operation for purging data from  Cosmos DB and
    /// Service Bus queues.
    /// </summary>
    /// <remarks>This controller is intended for administrative purposes and includes operations that can 
    /// significantly impact system data. Access to these endpoints should be restricted to authorized  users with
    /// appropriate credentials.</remarks>
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly Container _cosmosContainer;
        private readonly ServiceBusClient _serviceBusClient;
        private readonly IConfiguration _config;
        private readonly SecretClient _secretClient;
        private const string PURGEAUTHORIZATIONTOKENNAME = "PurgeAuthorizationToken";

        private static readonly string[] QueueNames = new[]
        {
        "transactions-inbound",
        "transactions-highrisk",
        "transactions-inbound/$DeadLetterQueue"
        };
        public AdminController(CosmosClient cosmosClient, ServiceBusClient serviceBusClient, IConfiguration config, SecretClient secret)
        {
            _cosmosContainer = cosmosClient.GetContainer("finque-cosmos", "Transactions");
            _serviceBusClient = serviceBusClient;
            _config = config;
            _secretClient = secret; 
        }

        /// <summary>
        /// Purges all queues defined in <see cref="QueueNames"/>.
        /// Purges CosmosDb Transactions table.
        /// </summary>
        /// <remarks>
        /// Requires token from Azure Keyvault to execute purge.
        /// </remarks>
        /// <returns>Purge complete message and purge counts.</returns>
        [HttpPost("purge")]
        public async Task<IActionResult> PurgeAll()
        {
            // SECURITY CRITICAL - DO NOT ALTER TOKEN REQUIREMENTS WITHOUT AUTHORIZATION!
            var secret = await _secretClient.GetSecretAsync(PURGEAUTHORIZATIONTOKENNAME);
            var purgeAuthorizationToken = secret.Value.Value.ToString();
            if (string.IsNullOrEmpty(purgeAuthorizationToken) ||
                !Request.Headers.TryGetValue("X-Admin-Token", out var providedToken) ||
                providedToken != purgeAuthorizationToken)
            {
                return Unauthorized("Missing or invalid admin token.");
            }

            var deletedCount = 0;
            var cosmosErrors = new List<string>();
            var sbErrors = new List<string>();

            // Purge Cosmos DB
            try
            {
                var query = _cosmosContainer.GetItemQueryIterator<dynamic>("SELECT c.id FROM c");
                while (query.HasMoreResults)
                {
                    var response = await query.ReadNextAsync();
                    foreach (var item in response)
                    {
                        try
                        {
                            string id = item.id;
                            await _cosmosContainer.DeleteItemAsync<dynamic>(id, new PartitionKey(id));
                            deletedCount++;
                        }
                        catch (Exception ex)
                        {
                            cosmosErrors.Add($"Failed to delete ID {item.id}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                cosmosErrors.Add($"Cosmos query failed: {ex.Message}");
            }

            // Purge Service Bus queues
            foreach (var queue in QueueNames)
            {
                try
                {
                    var receiver = _serviceBusClient.CreateReceiver(queue, new ServiceBusReceiverOptions
                    {
                        ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
                    });

                    while (true)
                    {
                        var messages = await receiver.ReceiveMessagesAsync(maxMessages: 50, maxWaitTime: TimeSpan.FromSeconds(2));
                        if (messages.Count == 0)
                            break;
                    }

                    await receiver.CloseAsync();
                }
                catch (Exception ex)
                {
                    sbErrors.Add($"Queue '{queue}' purge failed: {ex.Message}");
                }
            }

            return Ok(new
            {
                Message = "Purge complete.",
                CosmosDeleted = deletedCount,
                CosmosErrors = cosmosErrors,
                QueueErrors = sbErrors
            });
        }
    }
}
