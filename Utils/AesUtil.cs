using System.Security.Cryptography;
using System.Text;

namespace CloudreveDesktop.utils;

public static class AesUtil
{
    private const int KeySizeInBytes = 32; // 256-bit key
    private const int NonceSize = 12; // 96-bit nonce for GCM
    private const int TagSize = 16; // 128-bit authentication tag
    private const int Pbkdf2Iterations = 10000;

    /// <summary>
    ///     AES-GCM 加密方法：加密前将明文进行 Base64 编码
    /// </summary>
    public static string Encrypt(string plainText, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(KeySizeInBytes);
        var iv = RandomNumberGenerator.GetBytes(NonceSize);

        using var deriveBytes = new Rfc2898DeriveBytes(password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256);
        var key = deriveBytes.GetBytes(KeySizeInBytes);

        var aes = new AesGcm(key, TagSize);

        // 🔐 加密前进行 Base64 编码
        var plainTextBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
        var plaintextBytes = Encoding.UTF8.GetBytes(plainTextBase64);

        var cipherText = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        aes.Encrypt(iv, plaintextBytes, cipherText, tag);

        // Combine: Salt + IV + CipherText + Tag
        var combined = new byte[salt.Length + iv.Length + cipherText.Length + tag.Length];
        Buffer.BlockCopy(salt, 0, combined, 0, salt.Length);
        Buffer.BlockCopy(iv, 0, combined, salt.Length, iv.Length);
        Buffer.BlockCopy(cipherText, 0, combined, salt.Length + iv.Length, cipherText.Length);
        Buffer.BlockCopy(tag, 0, combined, salt.Length + iv.Length + cipherText.Length, tag.Length);

        return Convert.ToBase64String(combined);
    }

    /// <summary>
    ///     AES-GCM 解密方法：解密后自动还原 Base64 数据为原始明文
    /// </summary>
    public static string Decrypt(string base64Encrypted, string password)
    {
        var combined = Convert.FromBase64String(base64Encrypted);

        var salt = new byte[KeySizeInBytes];
        var iv = new byte[NonceSize];
        var cipherText = new byte[combined.Length - salt.Length - iv.Length - TagSize];
        var tag = new byte[TagSize];

        Buffer.BlockCopy(combined, 0, salt, 0, salt.Length);
        Buffer.BlockCopy(combined, salt.Length, iv, 0, iv.Length);
        Buffer.BlockCopy(combined, salt.Length + iv.Length, cipherText, 0, cipherText.Length);
        Buffer.BlockCopy(combined, salt.Length + iv.Length + cipherText.Length, tag, 0, tag.Length);

        using var deriveBytes = new Rfc2898DeriveBytes(password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256);
        var key = deriveBytes.GetBytes(KeySizeInBytes);

        var aes = new AesGcm(key, TagSize);

        var decrypted = new byte[cipherText.Length];

        try
        {
            aes.Decrypt(iv, cipherText, tag, decrypted);

            // 🔍 先转 UTF8 字符串，再用 Base64 解码回原始明文
            var plainTextBase64 = Encoding.UTF8.GetString(decrypted).TrimEnd('\0');
            var originalData = Convert.FromBase64String(plainTextBase64);
            return Encoding.UTF8.GetString(originalData);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException or ArgumentException)
        {
            throw new CryptographicException("解密失败，数据可能被篡改、密码错误，或不是有效的加密数据");
        }
    }
}