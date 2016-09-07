using System;
using System.IO;
using System.Security.Cryptography;

namespace RSA_AES_Handshake_Client
{
    class Crypto
    {
        public static byte[] aesSessionKey = null;
        public static byte[] aesSessionIV = null;

        public static byte[] RSADecrypt(byte[] toDecrypt, RSAParameters keyInfo, bool oaepPadding)
        {
            try
            {
                byte[] decryptedData;

                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.ImportParameters(keyInfo);
                    decryptedData = rsa.Decrypt(toDecrypt, oaepPadding);
                }

                return decryptedData;
            }
            catch (CryptographicException e)
            {
                Console.WriteLine("Error: {0}", e);
                return null;
            }
        }

        public static byte[] AESEncryptToBytes(string plainText, byte[] key, byte[] iv, int keySize = 256, int blockSize = 128)
        {
            //check arguments
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (key == null || key.Length <= 0)
                throw new ArgumentNullException("key");
            if (iv == null || iv.Length <= 0)
                throw new ArgumentNullException("iv");

            byte[] encrypted;

            //create Rijndael object (key size = 256, block size = 128 is valid AES)
            using (var aes = new RijndaelManaged { KeySize = keySize, BlockSize = blockSize})
            {
                aes.Key = key;
                aes.IV = iv;

                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV); //ICryptoTransform object

                //create the streams 
                using (var msEncrypt = new MemoryStream())
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                            swEncrypt.Write(plainText);

                        encrypted = msEncrypt.ToArray();
                    }
            }

            //return encrypted bytes from stream 
            return encrypted;
        }

        public static string AESDecryptFromBytes(byte[] cipherText, byte[] key, byte[] iv, int keySize = 256, int blockSize = 128)
        {
            //check arguments
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (key == null || key.Length <= 0)
                throw new ArgumentNullException("key");
            if (iv == null || iv.Length <= 0)
                throw new ArgumentNullException("iv");

            string plaintext;

            //create an aes object with the specified key and IV. 
            using (var aes = new RijndaelManaged { KeySize = keySize, BlockSize = blockSize })
            {
                aes.Key = key;
                aes.IV = iv;
                
                var decrypt = aes.CreateDecryptor(aes.Key, aes.IV); //ICryptoTransform object

                //create streams and decrypt 
                using (var msDecrypt = new MemoryStream(cipherText))
                    using (var csDecrypt = new CryptoStream(msDecrypt, decrypt, CryptoStreamMode.Read))
                        using (var srDecrypt = new StreamReader(csDecrypt))
                            plaintext = srDecrypt.ReadToEnd();
            }

            return plaintext;
        }
    }
}
