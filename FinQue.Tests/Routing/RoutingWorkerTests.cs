using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Moq;
using RoutingService;
using Shared.Messaging;
using Shared.Models;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class RoutingWorkerTests
{
    [Fact]
    public async Task Routes_HighRisk_ToFraudQueue()
    {
        // Arrange
        var fakeTx = new Transaction
        {
            Id = "txn1",
            Amount = 500,
            RiskScore = 95
        };

        var mockCosmos = new Mock<Microsoft.Azure.Cosmos.Container>();
        mockCosmos
            .Setup(c => c.ReadItemAsync<Transaction>(
                fakeTx.Id, It.IsAny<PartitionKey>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FakeItemResponse<Transaction>(fakeTx));

        mockCosmos
            .Setup(c => c.ReplaceItemAsync(
                It.IsAny<Transaction>(), fakeTx.Id, It.IsAny<PartitionKey>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FakeItemResponse<Transaction>(fakeTx));

        var mockSender = new Mock<ServiceBusSender>();
        mockSender
            .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockClient = new Mock<ServiceBusClient>();
        mockClient.Setup(c => c.CreateSender("fraud-queue")).Returns(mockSender.Object);

        var mockLogger = new Mock<ILogger<RoutingWorker>>();

        var worker = new RoutingWorker(mockClient.Object, MockCosmosClient(mockCosmos.Object), mockLogger.Object);

        // simulate a real message
        var message = new ServiceBusMessage(fakeTx.Id);
        var args = TestHelpers.CreateMessageEventArgs(message);

        // Act
        await worker.GetType()
            .GetMethod("HandleMessageAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(worker, new object[] { args });

        // Assert
        mockClient.Verify(c => c.CreateSender("fraud-queue"), Times.Once);
        mockSender.Verify(s => s.SendMessageAsync(It.Is<ServiceBusMessage>(m => m.Body.ToString() == fakeTx.Id), default), Times.Once);
    }

    private CosmosClient MockCosmosClient(Microsoft.Azure.Cosmos.Container container)
    {
        var mock = new Mock<CosmosClient>();
        mock.Setup(c => c.GetContainer(It.IsAny<string>(), It.IsAny<string>())).Returns(container);
        return mock.Object;
    }

    // Minimal stub for Cosmos ItemResponse<T>
    public class FakeItemResponse<T> : ItemResponse<T>
    {
        public FakeItemResponse(T resource) => Resource = resource;
        public override T Resource { get; }
    }
}
