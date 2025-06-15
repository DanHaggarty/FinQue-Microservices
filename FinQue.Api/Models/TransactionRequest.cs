namespace FinQue.Api.Models
{
    public class TransactionRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
    }
}
