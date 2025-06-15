
using Azure.Security.KeyVault.Secrets;

namespace FinQue.Api.Services
{
    /// <summary>
    /// Provides functionality to retrive Azure KeyVault seccret value by name
    /// </summary>
    public class SecretProvider : ISecretProvider
    {
        private readonly SecretClient _secretClient;
        private const string SecretName = "PurgeAuthorizationToken";

        public SecretProvider(SecretClient secretClient)
        {
            _secretClient = secretClient;
        }

        public async Task<string?> GetSecretValueAsync(string secretName)
        {
            try
            {
                var secret = await _secretClient.GetSecretAsync(SecretName);
                return secret?.Value?.Value;
            }
            catch
            {
                return null;
            }
        }
    }
}
