namespace AurionCal.Api.Services.Interfaces;

// <summary>
// Interface pour la gestion des secrets de cryptage
// Peut être implémentée par différents services de cryptage
// Exemple : KeyVaultService, AesService, etc.
// </summary>
public interface IEncryptionService
{
    // <summary>
    // Encrypte un texte en base64
    // </summary>
    // <param name="plainText">Le texte à encrypter</param>
    // <param name="cancellationToken">Le token d'annulation</param>
    // <returns>Le texte encrypé en base64</returns>
    Task<string> EncryptAsync(string plainText, CancellationToken cancellationToken = default);

    // <summary>
    // Décrypte un texte en base64
    // </summary>
    // <param name="cipherTextBase64">Le texte à décrypter</param>
    // <param name="cancellationToken">Le token d'annulation</param>
    // <returns>Le texte décrypé</returns>
    Task<string> DecryptAsync(string cipherTextBase64, CancellationToken cancellationToken = default);
}


