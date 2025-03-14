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
        #region AES 加密解密
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
        #endregion

        /// <summary>
        /// 密钥类
        /// </summary>
        /// <remarks>
        /// 构造函数
        /// </remarks>
        /// <param name="publicKey">共有key</param>
        /// <param name="privateKey">私有key</param>
        public class KeyModel(string publicKey, string privateKey)
        {
            /// <summary>
            /// 公有Key---加密key
            /// </summary>
            public string PublicKey { get; set; } = publicKey;

            /// <summary>
            /// 私有Key--可以加密也可以加密
            /// </summary>
            public string PrivateKey { get; set; } = privateKey;
        }
        /// <summary>
        /// Rsa加密 -- 非对称式
        /// </summary>
        public class RsaEncrypt
        {
            /// <summary>
            /// 获取公钥/私钥
            /// 每次生成的公钥和私钥都不一样
            /// </summary>
            /// <returns>Encrypt   Decrypt</returns>
            public static KeyModel GetKeyPair()
            {
                RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();

                //包括专用参数： 如果为true，则包含公钥和私钥RSA密钥；false，仅包括公众钥匙 
                string publicKey = RSA.ToXmlString(false); //生成的是公开的解密key
                string privateKey = RSA.ToXmlString(true); //生成的是私有的既可以加密也可以解密的key
                return new KeyModel(publicKey, privateKey);
            }

            /// <summary>
            /// RSA加密
            /// </summary>
            /// <param name="encryptText">待加密文本</param>
            /// <param name="encryptKey">加密key</param>
            /// <returns>加密后字符</returns>
            public static string Encrypt(string encryptText, string encryptKey)
            {
                RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(encryptKey);

                UnicodeEncoding ByteConverter = new UnicodeEncoding();
                //内容转成Unicode编码
                byte[] DataToEncrypt = ByteConverter.GetBytes(encryptText);
                //生成Rsa码
                byte[] resultBytes = rsa.Encrypt(DataToEncrypt, false);

                //字节转字符串
                return Convert.ToBase64String(resultBytes);
            }

            /// <summary>
            /// RSA解密
            /// </summary>
            /// <param name="decryptText">待解密文本</param>
            /// <param name="decryptKey">解密key</param>
            /// <returns>解密后字符</returns>
            public static string Decrypt(string decryptText, string decryptKey)
            {
                //字符转字节
                byte[] dataToDecrypt = Convert.FromBase64String(decryptText);

                RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
                RSA.FromXmlString(decryptKey);
                //解码
                byte[] resultBytes = RSA.Decrypt(dataToDecrypt, false);

                UnicodeEncoding ByteConverter = new UnicodeEncoding();
                //根据Unicode转化成字符串
                return ByteConverter.GetString(resultBytes);
            }

            /// <summary>
            /// 生成签名
            /// </summary>
            /// <param name="content">原文</param>
            /// <param name="privatekey">私钥</param>
            /// <returns>加密后字符</returns>
            public static string SignData(string content, string privatekey)
            {
                //字符转字节
                byte[] messagebytes = Encoding.UTF8.GetBytes(content);

                RSACryptoServiceProvider oRSA3 = new RSACryptoServiceProvider();
                oRSA3.FromXmlString(privatekey);

                //生成签名
                byte[] AOutput = oRSA3.SignData(messagebytes, "SHA1");
                //字节转字符
                return Convert.ToBase64String(AOutput);
            }

            /// <summary>
            /// 验证签名
            /// </summary>
            /// <param name="content">原文</param>
            /// <param name="autograph">签名</param>
            /// <param name="publickey">共有key</param>
            /// <returns>
            ///     true -- 验证成功
            ///     false -- 验证失败
            /// </returns>
            public static bool VerifyData(string content, string autograph, string publickey)
            {
                //字符转字节
                byte[] messagebytes = Encoding.UTF8.GetBytes(content);
                byte[] messageAutographbytes = Convert.FromBase64String(autograph);

                RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
                RSA.FromXmlString(publickey);

                //验证 content 和 autograph 是否相同
                bool bVerify = RSA.VerifyData(messagebytes, "SHA1", messageAutographbytes);
                return bVerify;
            }

            /// <summary>
            /// 生成公钥私钥，同时完成加密
            /// </summary>
            /// <param name="content"></param>
            /// <param name="publicKey">加密key</param>
            /// <param name="privateKey">解密key</param>
            /// <returns>加密后结果</returns>
            private static string Encrypt(string content, out string publicKey, out string privateKey)
            {
                RSACryptoServiceProvider rsaProvider = new RSACryptoServiceProvider();
                publicKey = rsaProvider.ToXmlString(false);
                privateKey = rsaProvider.ToXmlString(true);

                UnicodeEncoding ByteConverter = new UnicodeEncoding();
                byte[] DataToEncrypt = ByteConverter.GetBytes(content);
                byte[] resultBytes = rsaProvider.Encrypt(DataToEncrypt, false);
                return Convert.ToBase64String(resultBytes);
            }
            /// <summary>
            /// RSA加密
            /// </summary>
            /// <param name="str"></param>
            /// <param name="publicKey"></param>
            /// <returns></returns>
            public static string RSAEncrypt(string str, string publicKey)
            {
                using (RSACryptoServiceProvider RSACryptography = new RSACryptoServiceProvider())
                {
                    RSACryptography.FromXmlString(publicKey);
                    Byte[] PlaintextData = Encoding.UTF8.GetBytes(str);
                    int MaxBlockSize = RSACryptography.KeySize / 8 - 11;    //加密块最大长度限制

                    if (PlaintextData.Length <= MaxBlockSize)
                        return Convert.ToBase64String(RSACryptography.Encrypt(PlaintextData, false));

                    using (MemoryStream PlaiStream = new MemoryStream(PlaintextData))
                    using (MemoryStream CrypStream = new MemoryStream())
                    {
                        Byte[] Buffer = new Byte[MaxBlockSize];
                        int BlockSize = PlaiStream.Read(Buffer, 0, MaxBlockSize);

                        while (BlockSize > 0)
                        {
                            Byte[] ToEncrypt = new Byte[BlockSize];
                            Array.Copy(Buffer, 0, ToEncrypt, 0, BlockSize);

                            Byte[] Cryptograph = RSACryptography.Encrypt(ToEncrypt, false);
                            CrypStream.Write(Cryptograph, 0, Cryptograph.Length);

                            BlockSize = PlaiStream.Read(Buffer, 0, MaxBlockSize);
                        }

                        return Convert.ToBase64String(CrypStream.ToArray(), Base64FormattingOptions.None);
                    }
                }
            }

            /// <summary>
            /// RSA解密
            /// </summary>
            /// <param name="str"></param>
            /// <param name="privateKey"></param>
            /// <returns></returns>
            public static string RSADecrypt(string str, string privateKey)
            {
                using (RSACryptoServiceProvider RSACryptography = new RSACryptoServiceProvider())
                {
                    RSACryptography.FromXmlString(privateKey);
                    Byte[] CiphertextData = Convert.FromBase64String(str);
                    int MaxBlockSize = RSACryptography.KeySize / 8;    //解密块最大长度限制 117

                    if (CiphertextData.Length <= MaxBlockSize)
                        return Encoding.UTF8.GetString(RSACryptography.Decrypt(CiphertextData, false));

                    using (MemoryStream CrypStream = new MemoryStream(CiphertextData))
                    using (MemoryStream PlaiStream = new MemoryStream())
                    {
                        Byte[] Buffer = new Byte[MaxBlockSize];
                        int BlockSize = CrypStream.Read(Buffer, 0, MaxBlockSize);

                        while (BlockSize > 0)
                        {
                            Byte[] ToDecrypt = new Byte[BlockSize];
                            Array.Copy(Buffer, 0, ToDecrypt, 0, BlockSize);

                            Byte[] Plaintext = RSACryptography.Decrypt(ToDecrypt, false);
                            PlaiStream.Write(Plaintext, 0, Plaintext.Length);

                            BlockSize = CrypStream.Read(Buffer, 0, MaxBlockSize);
                        }

                        return Encoding.UTF8.GetString(PlaiStream.ToArray());
                    }
                }
            }

        }
    }
}
