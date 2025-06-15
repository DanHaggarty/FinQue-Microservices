using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc;
using Shared.Messaging;

namespace FinQue.Api.Controllers
{
    /// <summary>
    /// Controller for peeking messages from various queues.
    /// </summary>
    [ApiController]
    [Route("api/queues")]
    public class QueuePeekController : ControllerBase
    {
        private readonly ServiceBusClient _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueuePeekController"/> class.
        /// </summary>
        /// <param name="client">The Service Bus client used to interact with queues.</param>
        public QueuePeekController(ServiceBusClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Peek messages from the Validated queue.
        /// </summary>
        [HttpGet("validated")]
        public async Task<IActionResult> GetValidatedMessages()
            => await PeekQueue(QueueNames.Validated);

        /// <summary>
        /// Peek messages from the High Risk queue.
        /// </summary>
        [HttpGet("highrisk")]
        public async Task<IActionResult> GetHighRiskMessages()
            => await PeekQueue(QueueNames.HighRisk);

        /// <summary>
        /// Peek messages from the Dead Letter queue.
        /// </summary>
        [HttpGet("deadletter")]
        public async Task<IActionResult> GetDeadLetterMessages()
            => await PeekQueue(QueueNames.DeadLetter);

        /// <summary>
        /// Peeks messages from the specified queue.
        /// </summary>
        /// <param name="queueName">The name of the queue to peek messages from.</param>
        /// <returns>A list of messages from the queue.</returns>
        private async Task<IActionResult> PeekQueue(string queueName)
        {
            try
            {
                var receiver = _client.CreateReceiver(queueName);
                var messages = await receiver.PeekMessagesAsync(10);

                var results = messages.Select(msg => new
                {
                    msg.MessageId,
                    msg.Subject,
                    Body = msg.Body.ToString(),
                    msg.ApplicationProperties
                });

                return Ok(results);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
