using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace RSA_AES_Handshake_Server
{
    class Crypto
    {
        public static byte[] aesSessionKey;
        public static byte[] aesSessionIV;

        ///<summary>Generate a BASE64 encoded handshake token containing an AES key and initialization vector.</summary>
        public static byte[] GenerateHandshakeToken(int aesKeySize = 256) 
        {
            try
            {
                using (var aes = new RijndaelManaged {KeySize = aesKeySize})
                {
                    //set session key and iv
                    aesSessionKey = aes.Key;
                    aesSessionIV = aes.IV;

                    var hsToken = Encoding.UTF8.GetBytes(Convert.ToBase64String(aesSessionKey) + "||" + Convert.ToBase64String(aesSessionIV));

                    return hsToken;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error generating session keys: {0}", e.Message);
                return null;
            }
        }
        
        public static byte[] RSAEncrypt(byte[] toEncrypt, RSAParameters keyInfo, bool oaepPadding)
        {
            try
            {
                byte[] encryptedData;

                //create RSACryptoServiceProvider.
                using (var rsa = new RSACryptoServiceProvider())
                {
                    //load public key info & encrypt bytes (use OAEP padding)
                    rsa.ImportParameters(keyInfo);
                    encryptedData = rsa.Encrypt(toEncrypt, oaepPadding);
                }

                return encryptedData;
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
            using (var aes = new RijndaelManaged { KeySize = keySize, BlockSize = blockSize })
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
