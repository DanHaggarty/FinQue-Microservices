using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using FinQue.Api.Models;
using FinQue.Api.Services;
using Moq;
using Xunit;

namespace FinQue.Tests.Services
{
    /// <summary>
    /// Unit tests for the ServiceBusPublisher class.
    /// </summary>
    public class ServiceBusPublisherTests
    {
        private readonly Mock<ServiceBusClient> _mockClient;
        private readonly Mock<ServiceBusSender> _mockSender;
        private readonly ServiceBusPublisher _publisher;

        /// <summary>
        /// Initializes mocks and test instance of ServiceBusPublisher.
        /// </summary>
        public ServiceBusPublisherTests()
        {
            _mockClient = new Mock<ServiceBusClient>();
            _mockSender = new Mock<ServiceBusSender>();

            _mockClient
                .Setup(client => client.CreateSender(It.IsAny<string>()))
                .Returns(_mockSender.Object);

            _publisher = new ServiceBusPublisher(_mockClient.Object);
        }

        /// <summary>
        /// Verifies that PublishTransactionAsync correctly includes the Currency field in the serialized message body for multiple values.
        /// </summary>
        [Theory]
        [InlineData("EUR")]
        [InlineData("USD")]
        [InlineData("JPY")]
        public async Task PublishTransactionAsync_SerializesCurrencyCorrectly(string currency)
        {
            var request = new TransactionRequest
            {
                Amount = 123.45m,
                Currency = currency
            };

            await _publisher.PublishTransactionAsync(request);

            _mockSender.Verify(sender =>
                sender.SendMessageAsync(It.Is<ServiceBusMessage>(m => m.Body.ToString().Contains(currency)), default),
                Times.Once);
        }

        /// <summary>
        /// Verifies that the correct queue name is passed to CreateSender.
        /// </summary>
        [Fact]
        public async Task PublishTransactionAsync_CreatesSender_WithCorrectQueueName()
        {
            var request = new TransactionRequest
            {
                Amount = 100,
                Currency = "USD"
            };

            await _publisher.PublishTransactionAsync(request);

            _mockClient.Verify(client => client.CreateSender("transactions-inbound"), Times.Once);
        }

        /// <summary>
        /// Validates that the entire TransactionRequest is serialized and sent correctly.
        /// </summary>
        [Fact]
        public async Task PublishTransactionAsync_SerializesFullRequestCorrectly()
        {
            TransactionRequest original = new()
            {
                Amount = 200,
                Currency = "GBP"
            };

            ServiceBusMessage? capturedMessage = null;

            _mockSender
                .Setup(sender => sender.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default))
                .Callback<ServiceBusMessage, CancellationToken>((msg, _) => capturedMessage = msg);

            await _publisher.PublishTransactionAsync(original);

            Assert.NotNull(capturedMessage);

            var deserialized = JsonSerializer.Deserialize<TransactionRequest>(capturedMessage!.Body);

            Assert.Equal(original.Amount, deserialized.Amount);
            Assert.Equal(original.Currency, deserialized.Currency);
        }


        /// <summary>
        /// Simulates a failure when sending to Service Bus and ensures the exception is propagated.
        /// </summary>
        [Fact]
        public async Task PublishTransactionAsync_Throws_WhenSendFails()
        {
            var request = new TransactionRequest
            {
                Amount = 100,
                Currency = "USD"
            };

            _mockSender
                .Setup(sender => sender.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default))
                .ThrowsAsync(new ServiceBusException("Send failed", ServiceBusFailureReason.GeneralError));

            await Assert.ThrowsAsync<ServiceBusException>(() => _publisher.PublishTransactionAsync(request));
        }
    }
}

