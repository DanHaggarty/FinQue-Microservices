using Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transaction = Shared.Models.Transaction;

namespace ValidationService.Validators
{
    public interface IMessageValidator
    {
        /// <summary>
        /// Validates the message and returns a boolean indicating whether it is valid.
        /// </summary>
        /// <param name="message">The message to validate.</param>
        /// <returns>True if the message is valid, otherwise false.</returns>
        bool IsValid(Transaction message, out List<string> reasons);
        /// <summary>
        /// Gets the validation errors if any.
        /// </summary>
        /// <returns>A list of validation error messages.</returns>
        IEnumerable<string> GetValidationErrors();
    }
}
