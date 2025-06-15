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
    /// Provides functionality to validate transactions and retrieve validation errors.
    /// </summary>
    /// <remarks>This class validates transactions based on specific rules, such as ensuring the transaction
    /// ID is not null or empty and that the transaction amount is greater than zero. Use <see cref="IsValid"/> to
    /// perform validation and retrieve detailed error messages if the transaction is invalid.</remarks>
    public class TransactionValidator : IMessageValidator
    {
        private readonly List<string> _errors = new();

        public IEnumerable<string> GetValidationErrors() => _errors;

        public bool IsValid(Transaction message, out List<string> reasons)
        {
            _errors.Clear();

            if (string.IsNullOrWhiteSpace(message.id))
                _errors.Add("Missing transaction ID.");

            if (message.Amount <= 0)
                _errors.Add("Amount must be greater than zero.");

            reasons = _errors.ToList(); // Defensive copy
            return !_errors.Any();
        }
    }
}
