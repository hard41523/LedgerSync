using System;
using System.Security.Cryptography;
using System.Text;

namespace LedgerSyncViewModel.Helper
{
    /// <summary>
    /// Encrypts/decrypts sensitive strings using Windows DPAPI.
    /// Data is tied to the current Windows user account — only the same user
    /// on the same machine can decrypt it.
    /// </summary>
    public static class CryptoHelper
    {
        // Optional entropy adds an extra application-specific secret on top of DPAPI
        private static readonly byte[] _entropy = Encoding.UTF8.GetBytes("LedgerSync_v1_entropy");

        /// <summary>
        /// Encrypts a plain-text string and returns a Base64-encoded cipher string.
        /// </summary>
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipherBytes = ProtectedData.Protect(plainBytes, _entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(cipherBytes);
        }

        /// <summary>
        /// Decrypts a Base64-encoded cipher string back to plain text.
        /// Returns null if decryption fails (e.g. data was stored without encryption).
        /// </summary>
        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                byte[] plainBytes = ProtectedData.Unprotect(cipherBytes, _entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                // Stored value was not encrypted (legacy row) — return as-is
                return cipherText;
            }
        }
    }
}
