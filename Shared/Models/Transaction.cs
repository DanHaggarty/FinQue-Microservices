namespace Shared.Models;

public class Transaction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FromAccount { get; set; } = string.Empty;
    public string ToAccount { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int RiskScore { get; set; } = 0;
    public List<string> Tags { get; set; } = new();
}
