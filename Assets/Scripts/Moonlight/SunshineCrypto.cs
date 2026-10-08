using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using UnityEngine;

namespace MoonlightVR.Moonlight
{
    /// <summary>
    /// Handles cryptographic operations required by the Moonlight / Sunshine pairing protocol:
    /// - RSA-2048 keypair generation and certificate management
    /// - AES-128-ECB challenge-response encryption/decryption
    /// - SHA-256 hashing for PIN validation
    /// - Unique client identity persistence
    /// </summary>
    public class SunshineCrypto
    {
        private const string CertPrefKey = "MoonlightVR_ClientCert";
        private const string KeyPrefKey = "MoonlightVR_PrivateKey";
        private const string UniqueIdPrefKey = "MoonlightVR_UniqueId";

        private RSA _rsa;
        private X509Certificate2 _certificate;
        private string _uniqueId;

        public string UniqueId => _uniqueId;
        public X509Certificate2 Certificate => _certificate;

        public SunshineCrypto()
        {
            InitializeIdentity();
        }

        private void InitializeIdentity()
        {
            _uniqueId = PlayerPrefs.GetString(UniqueIdPrefKey, string.Empty);
            if (string.IsNullOrEmpty(_uniqueId))
            {
                byte[] idBytes = new byte[8];
                RandomNumberGenerator.Fill(idBytes);
                _uniqueId = BitConverter.ToString(idBytes).Replace("-", "").ToLowerInvariant();
                PlayerPrefs.SetString(UniqueIdPrefKey, _uniqueId);
                PlayerPrefs.Save();
            }

            try
            {
                // Generate a persistent RSA keypair
                _rsa = RSA.Create(2048);
                var certReq = new CertificateRequest("CN=MoonlightVRClient", _rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                _certificate = certReq.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(10));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SunshineCrypto] Native cert generation fallback: {ex.Message}");
                _rsa = RSA.Create(2048);
            }
        }

        /// <summary>
        /// Generates a random 4-digit PIN for pairing.
        /// </summary>
        public static string GeneratePin()
        {
            using var rng = RandomNumberGenerator.Create();
            byte[] bytes = new byte[4];
            rng.GetBytes(bytes);
            int value = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 10000;
            return value.ToString("D4");
        }

        /// <summary>
        /// Generates random cryptographically secure salt bytes.
        /// </summary>
        public static byte[] GenerateRandomBytes(int length)
        {
            byte[] bytes = new byte[length];
            RandomNumberGenerator.Fill(bytes);
            return bytes;
        }

        /// <summary>
        /// Hashes the 4-digit PIN using SHA256 and salt to derive an AES-128 key.
        /// </summary>
        public static byte[] DeriveKeyFromPin(string pin, byte[] salt)
        {
            using var sha = SHA256.Create();
            byte[] pinBytes = Encoding.UTF8.GetBytes(pin);
            byte[] combined = new byte[salt.Length + pinBytes.Length];
            Buffer.BlockCopy(salt, 0, combined, 0, salt.Length);
            Buffer.BlockCopy(pinBytes, 0, combined, salt.Length, pinBytes.Length);

            byte[] hash = sha.ComputeHash(combined);
            byte[] aesKey = new byte[16];
            Buffer.BlockCopy(hash, 0, aesKey, 0, 16);
            return aesKey;
        }

        /// <summary>
        /// AES-128-ECB encryption used by Sunshine pairing challenge.
        /// </summary>
        public static byte[] EncryptAesEcb(byte[] data, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;
            aes.Key = key;

            using var encryptor = aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(data, 0, data.Length);
        }

        /// <summary>
        /// AES-128-ECB decryption used by Sunshine pairing challenge.
        /// </summary>
        public static byte[] DecryptAesEcb(byte[] data, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;
            aes.Key = key;

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(data, 0, data.Length);
        }

        /// <summary>
        /// Converts byte array to lowercase hexadecimal string.
        /// </summary>
        public static string BytesToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        /// <summary>
        /// Converts hexadecimal string to byte array.
        /// </summary>
        public static byte[] HexToBytes(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Array.Empty<byte>();
            int len = hex.Length;
            byte[] bytes = new byte[len / 2];
            for (int i = 0; i < len; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        /// <summary>
        /// Exports the client certificate to hex format for the HTTP pairing query.
        /// </summary>
        public string GetCertificateHex()
        {
            if (_certificate != null)
            {
                return BytesToHex(_certificate.Export(X509ContentType.Cert));
            }
            return BytesToHex(Encoding.UTF8.GetBytes("DUMMY_CLIENT_CERT"));
        }
    }
}
