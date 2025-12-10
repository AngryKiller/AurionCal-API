using System.Security.Cryptography;
using System.Text;
using AurionCal.Api.Services.Interfaces;

namespace AurionCal.Api.Services;

/// <summary>
/// Implémentation locale de <see cref="IEncryptionService"/> basée sur une clé symétrique (AES).
/// La clé est lue depuis la configuration (Encryption:Key) ou une variable d'environnement.
/// </summary>
public class LocalEncryptionService : IEncryptionService
{
    private readonly byte[] _keyBytes;

    public LocalEncryptionService(IConfiguration configuration)
    {
        var key = Environment.GetEnvironmentVariable("ENCRYPTION_KEY")
                  ?? configuration["Encryption:Key"]
                  ?? throw new InvalidOperationException("Aucune clé de chiffrement trouvée. Définissez ENCRYPTION_KEY ou Encryption:Key dans la configuration.");

        // On accepte soit une clé déjà encodée en Base64, soit une chaîne texte classique qu'on hache.
        if (!TryGetKeyFromBase64(key, out _keyBytes))
        {
            // Dérive une clé 256 bits depuis la chaîne fournie (PBKDF2 pour avoir une taille fixe et suffisante).
            using var deriveBytes = new Rfc2898DeriveBytes(key, Encoding.UTF8.GetBytes("AurionCal-Salt"), 100_000, HashAlgorithmName.SHA256);
            _keyBytes = deriveBytes.GetBytes(32); // 256 bits
        }
    }

    public async Task<string> EncryptAsync(string plainText, CancellationToken cancellationToken = default)
    {
        if (plainText == null) throw new ArgumentNullException(nameof(plainText));

        using var aes = Aes.Create();
        aes.Key = _keyBytes;
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

        return await Task.FromResult(Convert.ToBase64String(result));
    }

    public async Task<string> DecryptAsync(string cipherTextBase64, CancellationToken cancellationToken = default)
    {
        if (cipherTextBase64 == null) throw new ArgumentNullException(nameof(cipherTextBase64));

        var fullCipher = Convert.FromBase64String(cipherTextBase64);
        using var aes = Aes.Create();
        aes.Key = _keyBytes;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var ivSize = aes.BlockSize / 8;
        var iv = new byte[ivSize];
        var cipherBytes = new byte[fullCipher.Length - ivSize];

        Buffer.BlockCopy(fullCipher, 0, iv, 0, ivSize);
        Buffer.BlockCopy(fullCipher, ivSize, cipherBytes, 0, cipherBytes.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

        return await Task.FromResult(Encoding.UTF8.GetString(plainBytes));
    }

    private static bool TryGetKeyFromBase64(string key, out byte[] keyBytes)
    {
        try
        {
            var raw = Convert.FromBase64String(key);
            if (raw.Length == 32)
            {
                keyBytes = raw;
                return true;
            }
        }
        catch
        {
            // pas du Base64 valide, on tombera sur la dérivation de clé
        }

        keyBytes = Array.Empty<byte>();
        return false;
    }
}

