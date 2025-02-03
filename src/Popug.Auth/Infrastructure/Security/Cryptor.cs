using System.Security.Cryptography;
using System.Text;
using Popug.Auth.Data;

namespace Popug.Auth.Infrastructure.Security;

public class Cryptor
{
    private static readonly HashAlgorithmName HashAlgoName = HashAlgorithmName.SHA512;
    private readonly string _password;

    private const int Keysize = 256;
    private const int BlockSize = 128;
    private const int DerivationIterations = 1000;
    

    public Cryptor(IConfiguration configuration)
    {
        _password = configuration.GetSection(nameof(Cryptor))["Password"]!;
    }

    public string Encrypt(string plainText)
    {
        // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
        // so that the same Salt and IV values can be used when decrypting.  
        var saltStringBytes = GenerateRandomEntropy();
        var ivStringBytes = GenerateRandomEntropy();
        var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        using var password = new Rfc2898DeriveBytes(_password, saltStringBytes, DerivationIterations, HashAlgoName);
        var keyBytes = password.GetBytes(Keysize / 8);
        using var symmetricKey = Aes.Create();
        symmetricKey.BlockSize = BlockSize;
        symmetricKey.Mode = CipherMode.ECB;
        symmetricKey.Padding = PaddingMode.PKCS7;
        using var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes);
        using var memoryStream = new MemoryStream();
        using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
        cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
        cryptoStream.FlushFinalBlock();
        // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
        var cipherTextBytes = saltStringBytes;
        cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
        cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
        memoryStream.Close();
        cryptoStream.Close();
        return Convert.ToBase64String(cipherTextBytes);
    }

    public string Decrypt(string cipherText)
    {
        // Get the complete stream of bytes that represent:
        // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
        var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
        // Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
        var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(BlockSize / 8).ToArray();
        // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
        var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(BlockSize / 8).Take(BlockSize / 8).ToArray();
        // Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
        var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((BlockSize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((BlockSize / 8) * 2)).ToArray();

        using var password = new Rfc2898DeriveBytes(_password, saltStringBytes, DerivationIterations, HashAlgoName);
        var keyBytes = password.GetBytes(Keysize / 8);
        using var symmetricKey = Aes.Create();
        symmetricKey.BlockSize = BlockSize;
        symmetricKey.Mode = CipherMode.ECB;
        symmetricKey.Padding = PaddingMode.PKCS7;
        using var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes);
        using var memoryStream = new MemoryStream(cipherTextBytes);
        using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
        var plainTextBytes = new byte[cipherTextBytes.Length];
        int decryptedByteCount;
        var total = 0;
        while ((decryptedByteCount = cryptoStream.Read(plainTextBytes, total,
                   plainTextBytes.Length - total)) > 0)
        {
            total += decryptedByteCount;
        }
                                
        memoryStream.Close();
        cryptoStream.Close();
        return Encoding.UTF8.GetString(plainTextBytes, 0, total);
    }

    private static byte[] GenerateRandomEntropy()
    {
        var randomBytes = new byte[BlockSize / 8];
        using (var rngCsp = RandomNumberGenerator.Create())
        {
            // Fill the array with cryptographically secure random bytes.
            rngCsp.GetBytes(randomBytes);
        }
        return randomBytes;
    }

    public string GetHash(string input)
    {
        byte[] data = SHA512.HashData(Encoding.UTF8.GetBytes(input));
        var sBuilder = new StringBuilder();
        
        for (int i = 0; i < data.Length; i++)
            sBuilder.Append(data[i].ToString("x2"));
        
        return sBuilder.ToString();
    }
    
    public bool VerifyHash(string input, string hash)
    {
        var hashOfInput = GetHash(input);
        var comparer = StringComparer.OrdinalIgnoreCase;
        return comparer.Compare(hashOfInput, hash) == 0;
    }
}