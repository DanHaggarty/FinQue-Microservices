using Microsoft.AspNetCore.Mvc;
using FinQue.Api.Models;
using FinQue.Api.Services;

namespace FinQue.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly ServiceBusPublisher _publisher;

        public TransactionsController(ServiceBusPublisher publisher)
        {
            _publisher = publisher;
        }

        [HttpPost]
        public async Task<IActionResult> PostTransaction(TransactionRequest request)
        {
            if (string.IsNullOrEmpty(request.Currency) || request.Amount <= 0)
            {
                return BadRequest("Invalid transaction payload");
            }

            await _publisher.PublishTransactionAsync(request);
            return Accepted(new { message = "Transaction submitted" });
        }
    }
}
