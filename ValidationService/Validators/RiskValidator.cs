using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Transaction = Shared.Models.Transaction;

namespace ValidationService.Validators
{
    /// <summary>
    /// Provides functionality to validate transactions for potential risks.
    /// </summary>
    /// <remarks>This class implements the <see cref="IMessageValidator"/> interface to validate transaction
    /// messages. It checks for specific conditions, such as amount > 1000 and if currency is crypto, then collects
    /// validation errors if any issues are found.</remarks>
    public class RiskValidator : IMessageValidator
    {
        private readonly List<string> _errors = new();
        private static readonly HashSet<string> _cryptoCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "BTC", "ETH", "USDT", "BNB", "XRP", "SOL", "ADA", "DOGE", "DOT", "TRX"
        };
        public IEnumerable<string> GetValidationErrors() => _errors;
        public bool IsValid(Transaction message, out List<string> reasons)
        {
            _errors.Clear();

            if (message.Amount > 1000)
                _errors.Add("Transaction contains high risk amount.");

            if (_cryptoCodes.Contains(message.Currency.Trim()))
                _errors.Add("Transaction contains a high-risk cryptocurrency.");

            reasons = _errors.ToList();
            return !_errors.Any();
        }
    }
}
