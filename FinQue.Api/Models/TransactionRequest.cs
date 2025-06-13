namespace FinQue.Api.Models
{
    public class TransactionRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
    }
}
