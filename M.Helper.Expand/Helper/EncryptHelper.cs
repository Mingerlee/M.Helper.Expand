using NPOI.POIFS.Crypt;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace M.Helper.Expand
{
    public class EncryptHelper
    {
        /// <summary>
        ///     AES 加密
        /// </summary>
        /// <param name="encryptText">待加密密文</param>
        /// <param name="encryptKey">加密密钥</param>
        /// <returns>加密的字符串</returns>
        public static string Encrypt(string encryptText, string encryptKey)
        {
            if (string.IsNullOrEmpty(encryptText)) { throw (new Exception("密文不得为空")); }
            if (string.IsNullOrEmpty(encryptKey)) { throw (new Exception("密钥不得为空")); }
            if (!encryptKey.Length.Equals(32)) throw new Exception("密钥必须32位");

            string m_strEncrypt = "";
            byte[] m_btIV = Convert.FromBase64String("Rkb4jvUy/ye7Cd7k89QQgQ==");
            Aes m_AESProvider = Aes.Create();
            
            try
            {
                byte[] m_btEncryptString = Encoding.Default.GetBytes(encryptText);
                MemoryStream m_stream = new MemoryStream();
                CryptoStream m_csstream = new CryptoStream(m_stream, m_AESProvider.CreateEncryptor(Encoding.Default.GetBytes(encryptKey), m_btIV), CryptoStreamMode.Write);
                m_csstream.Write(m_btEncryptString, 0, m_btEncryptString.Length); m_csstream.FlushFinalBlock();
                m_strEncrypt = Convert.ToBase64String(m_stream.ToArray());
                m_stream.Close(); m_stream.Dispose();
                m_csstream.Close(); m_csstream.Dispose();
            }
            catch (IOException ex) { throw ex; }
            catch (CryptographicException ex) { throw ex; }
            catch (ArgumentException ex) { throw ex; }
            catch (Exception ex) { throw ex; }
            finally { m_AESProvider.Clear(); }
            return m_strEncrypt;
        }

        /// <summary>
        /// AES 解密(高级加密标准，是下一代的加密算法标准，速度快，安全级别高，目前 AES 标准的一个实现是 Rijndael 算法)
        /// </summary>
        /// <param name="decryptText">待解密密文</param>
        /// <param name="decryptKey">解密密钥</param>
        /// <returns>解密的字符</returns>
        public static string Decrypt(string decryptText, string decryptKey)
        {
            if (string.IsNullOrEmpty(decryptText)) { throw (new Exception("密文不得为空")); }
            if (string.IsNullOrEmpty(decryptKey)) { throw (new Exception("密钥不得为空")); }
            if (!decryptKey.Length.Equals(32)) throw new Exception("密钥必须32位");

            string m_strDecrypt = "";
            byte[] m_btIV = Convert.FromBase64String("Rkb4jvUy/ye7Cd7k89QQgQ==");
            Rijndael m_AESProvider = Rijndael.Create();
            try
            {
                byte[] m_btDecryptString = Convert.FromBase64String(decryptText);
                MemoryStream m_stream = new MemoryStream();
                CryptoStream m_csstream = new CryptoStream(m_stream, m_AESProvider.CreateDecryptor(Encoding.Default.GetBytes(decryptKey), m_btIV), CryptoStreamMode.Write);
                m_csstream.Write(m_btDecryptString, 0, m_btDecryptString.Length); m_csstream.FlushFinalBlock();
                m_strDecrypt = Encoding.Default.GetString(m_stream.ToArray());
                m_stream.Close(); m_stream.Dispose();
                m_csstream.Close(); m_csstream.Dispose();
            }
            catch (IOException ex) { throw ex; }
            catch (CryptographicException ex) { throw ex; }
            catch (ArgumentException ex) { throw ex; }
            catch (Exception ex) { throw ex; }
            finally { m_AESProvider.Clear(); }
            return m_strDecrypt;
        }

        private static string key = "1234567890123456"; // 16字节密钥，AES-128
        private static string iv = "1234567890123456";  // 16字节IV（初始化向量）
        public static string Encrypt(string plainText)
        {
            using Aes aesAlg = Aes.Create(); // 创建AES加密实例
            aesAlg.Key = Encoding.UTF8.GetBytes(key);  // 设置密钥
            aesAlg.IV = Encoding.UTF8.GetBytes(iv);    // 设置初始化向量

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV); // 创建加密器

            var rand = RandomNumberGenerator.Create();
            byte[] bytes = new byte[32];
            rand.GetBytes(bytes);

            using MemoryStream msEncrypt = new MemoryStream();  // 用于存储加密后的数据
            using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))  // 创建加密数据流
            {
                using StreamWriter swEncrypt = new StreamWriter(csEncrypt);  // 写入加密流
                swEncrypt.Write(plainText);  // 写入明文数据
            }
            // 返回加密后的数据，转换为Base64字符串
            return Convert.ToBase64String(msEncrypt.ToArray());
        }

        // 解密函数
        public static string Decrypt(string cipherText)
        {
            using Aes aesAlg = Aes.Create(); // 创建AES实例
            aesAlg.Key = Encoding.UTF8.GetBytes(key);  // 设置密钥
            aesAlg.IV = Encoding.UTF8.GetBytes(iv);    // 设置初始化向量

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);  // 创建解密器

            using MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText));  // 将Base64字符串转换为字节数组并读取
            using CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);  // 创建解密数据流
            using StreamReader srDecrypt = new StreamReader(csDecrypt);  // 从解密流中读取数据
            return srDecrypt.ReadToEnd();  // 返回解密后的明文
        }
    }
}
