using Azure.Messaging.ServiceBus;
using System.Text.Json;
using FinQue.Api.Models;

namespace FinQue.Api.Services
{
    public class ServiceBusPublisher
    {
        private readonly ServiceBusClient _client;
        private readonly string _queueName = "transactions-inbound";

        public ServiceBusPublisher(ServiceBusClient client)
        {
            _client = client;
        }

        public async Task PublishTransactionAsync(TransactionRequest request)
        {
            var sender = _client.CreateSender(_queueName);
            var json = JsonSerializer.Serialize(request);
            var message = new ServiceBusMessage(json);
            await sender.SendMessageAsync(message);
        }
    }
}
