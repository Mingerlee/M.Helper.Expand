using M.Simple.Expand.Models;
using System.Security.Cryptography;
using System.Text;

namespace M.Simple.Helper
{
    /// <summary>
    ///     <para>M.Simple.Helper帮助类</para>
    ///     <para>AES加密解密帮助类</para>
    /// </summary>
    internal class AESEncryptHelper
    {
        private string Key { get; set; }
        private string Iv { get; set; }
        private static int[] keyByte = [16, 24, 32];

        public AESEncryptHelper(string key, string iv)
        {
            if (!(key.Length == 16 || key.Length == 24 || key.Length == 32))
            {
                throw new ArgumentException("The key character length must be equal to 16/24/32");
            }
            if (iv.Length != 16)
            {
                throw new ArgumentException("The iv character length must be equal to 16");
            }
            Key = key;
            Iv = iv;
        }
        #region AES 加密解密
        /// <summary>
        ///     AES 加密(随机秘钥和初始化向量)
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns>返回加密后的密文、秘钥、初始化向量</returns>
        public static AESEncryptResultModel AesEncryptReturnKey(string plainText)
        {
            using Aes aesAlg = Aes.Create(); // 创建AES加密实例

            var rd = new Random();

            var rand = RandomNumberGenerator.Create();
            byte[] bytesKey = new byte[keyByte[rd.Next(keyByte.Length)]];
            byte[] bytesIV = new byte[16];
            rand.GetBytes(bytesKey);
            rand.GetBytes(bytesIV);

            //aesAlg.Key = Encoding.UTF8.GetBytes(key);  // 设置密钥
            //aesAlg.IV = Encoding.UTF8.GetBytes(iv);    // 设置初始化向量
            aesAlg.Key = bytesKey;  // 设置密钥
            aesAlg.IV = bytesIV;    // 设置初始化向量

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV); // 创建加密器

            using MemoryStream msEncrypt = new MemoryStream();  // 用于存储加密后的数据
            using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))  // 创建加密数据流
            {
                using StreamWriter swEncrypt = new StreamWriter(csEncrypt);  // 写入加密流
                swEncrypt.Write(plainText);  // 写入明文数据
            }
            // 返回加密后的数据，转换为Base64字符串
            return new AESEncryptResultModel { CipherText = Convert.ToBase64String(msEncrypt.ToArray()), Key = bytesKey, IV = bytesIV };
        }
        /// <summary>
        ///     AES 加密
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns></returns>
        public string AesEncrypt(string plainText)
        {
            using Aes aesAlg = Aes.Create(); // 创建AES加密实例

            aesAlg.Key = Encoding.UTF8.GetBytes(Key);  // 设置密钥
            aesAlg.IV = Encoding.UTF8.GetBytes(Iv);    // 设置初始化向量

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV); // 创建加密器

            using MemoryStream msEncrypt = new();  // 用于存储加密后的数据
            using (CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write))  // 创建加密数据流
            {
                using StreamWriter swEncrypt = new StreamWriter(csEncrypt);  // 写入加密流
                swEncrypt.Write(plainText);  // 写入明文数据
            }
            // 返回加密后的数据，转换为Base64字符串
            return Convert.ToBase64String(msEncrypt.ToArray());
        }
        /// <summary>
        ///     AES 加密
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="key"></param>
        /// <param name="iv"></param>
        /// <returns></returns>
        public static string AesEncrypt(string plainText, byte[] key, byte[] iv)
        {
            if (!(key.Length == 16 || key.Length == 24 || key.Length == 32))
            {
                throw new ArgumentException("The key character length must be equal to 16/24/32");
            }
            if (iv.Length != 16)
            {
                throw new ArgumentException("The iv character length must be equal to 16");
            }

            using Aes aesAlg = Aes.Create(); // 创建AES加密实例

            aesAlg.Key = key;  // 设置密钥
            aesAlg.IV = iv;    // 设置初始化向量

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV); // 创建加密器

            using MemoryStream msEncrypt = new();  // 用于存储加密后的数据
            using (CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write))  // 创建加密数据流
            {
                using StreamWriter swEncrypt = new StreamWriter(csEncrypt);  // 写入加密流
                swEncrypt.Write(plainText);  // 写入明文数据
            }
            // 返回加密后的数据，转换为Base64字符串
            return Convert.ToBase64String(msEncrypt.ToArray());
        }
        /// <summary>
        ///     AES 解密
        /// </summary>
        /// <param name="cipherText"></param>
        /// <param name="key"></param>
        /// <param name="iv"></param>
        /// <returns></returns>
        public static string AesDecrypt(string cipherText, byte[] key, byte[] iv)
        {
            try
            {
                if (!(key.Length == 16 || key.Length == 24 || key.Length == 32))
                {
                    throw new ArgumentException("The key character length must be equal to 16/24/32");
                }
                if (iv.Length != 16)
                {
                    throw new ArgumentException("The iv character length must be equal to 16");
                }

                using Aes aesAlg = Aes.Create(); // 创建AES实例
                aesAlg.Key = key;  // 设置密钥
                aesAlg.IV = iv;    // 设置初始化向量

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);  // 创建解密器

                using MemoryStream msDecrypt = new(Convert.FromBase64String(cipherText));  // 将Base64字符串转换为字节数组并读取
                using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);  // 创建解密数据流
                using StreamReader srDecrypt = new(csDecrypt);  // 从解密流中读取数据
                return srDecrypt.ReadToEnd();  // 返回解密后的明文
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }
        /// <summary>
        ///     AES 解密
        /// </summary>
        /// <param name="cipherText"></param>
        /// <returns></returns>
        public string AesDecrypt(string cipherText)
        {
            try
            {
                using Aes aesAlg = Aes.Create(); // 创建AES实例
                aesAlg.Key = Encoding.UTF8.GetBytes(Key);  // 设置密钥
                aesAlg.IV = Encoding.UTF8.GetBytes(Iv);    // 设置初始化向量

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);  // 创建解密器

                using MemoryStream msDecrypt = new(Convert.FromBase64String(cipherText));  // 将Base64字符串转换为字节数组并读取
                using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);  // 创建解密数据流
                using StreamReader srDecrypt = new(csDecrypt);  // 从解密流中读取数据
                return srDecrypt.ReadToEnd();  // 返回解密后的明文
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }
        #endregion
    }
}
