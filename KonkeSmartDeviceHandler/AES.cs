using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace KonkeSmartDeviceHandler
{
    static class AES
    {
        // Konke devices' AES key, obtained from this exploit
        // https://github.com/BuddhaLabs/PacketStorm-Exploits/blob/a1b67645f5050f9946e48c8282023ab2f696130d/1506-exploits/kankun-disclose.txt
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("fdsl;mewrjope456fds4fbvfnjwaugfo");

        public static byte[] EncryptMessage(string msg)
        {
            using (var aes = Aes.Create("AesCryptoServiceProvider"))
            {
                aes.Key = Key;
                aes.Mode = CipherMode.ECB;
                aes.IV = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                aes.Padding = PaddingMode.Zeros;

                var encryptor = aes.CreateEncryptor();
                var msgBytes = Encoding.UTF8.GetBytes(msg);
                return encryptor.TransformFinalBlock(msgBytes, 0, msgBytes.Length);
            }
        }

        public static string DecryptMessage(byte[] encrypted)
        {
            using (var aes = Aes.Create("AesCryptoServiceProvider"))
            {
                aes.Key = Key;
                aes.Mode = CipherMode.ECB;
                aes.IV = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                aes.Padding = PaddingMode.Zeros;

                var decryptor = aes.CreateDecryptor();
                var decrypted = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
                return Encoding.UTF8.GetString(decrypted);
            }
        }
    }
}
