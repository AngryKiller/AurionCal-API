using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using AurionCal.Api.Services.Interfaces;

namespace AurionCal.Api.Services;

public class KeyVaultService : IEncryptionService, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly KeyClient _keyClient;
    private readonly TokenCredential _credential;
    
    
    public KeyVaultService(IConfiguration configuration)
    {
        _configuration = configuration;
        _credential = new DefaultAzureCredential();
        _keyClient = new KeyClient(new Uri(_configuration["KeyVault:KeyVaultUrl"]), _credential);
    }

    public async Task<string> EncryptAsync(string secret, CancellationToken c)
    {
        var plainText = System.Text.Encoding.UTF8.GetBytes(secret);
        KeyVaultKey key = await _keyClient.GetKeyAsync(_configuration["KeyVault:KeyName"], cancellationToken:c);
        var cryptoClient = new CryptographyClient(key.Id, _credential);
        var encryptResult = await cryptoClient.EncryptAsync(EncryptionAlgorithm.RsaOaep256, plainText, c);
        return Convert.ToBase64String(encryptResult.Ciphertext);
    }
    
    public async Task<string> DecryptAsync(string encryptedSecret, CancellationToken c)
    {
        var cipherText = Convert.FromBase64String(encryptedSecret);
        KeyVaultKey key = await _keyClient.GetKeyAsync(_configuration["KeyVault:KeyName"], cancellationToken:c);
        var cryptoClient = new CryptographyClient(key.Id, _credential);
        var decryptResult = await cryptoClient.DecryptAsync(EncryptionAlgorithm.RsaOaep256, cipherText, c);
        return System.Text.Encoding.UTF8.GetString(decryptResult.Plaintext);
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }

}