namespace FinQue.Api.Services
{
    public interface ISecretProvider
    {
        Task<string?> GetSecretValueAsync(string secretName);
    }
}
