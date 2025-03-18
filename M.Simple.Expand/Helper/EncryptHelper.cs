using M.Simple.Expand.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Unicode;

namespace M.Simple.Helper
{
    /// <summary>
    ///     <para>M.Simple.Helper帮助类</para>
    ///     <para>AES对称式加密解密帮助类</para>
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
    /// <summary>
    ///     <para>M.Simple.Helper帮助类</para>
    ///     <para>RSA非对称式加密解密帮助类</para>
    /// </summary>
    internal class RSAEncryptHelper
    {
        /// <summary>
        /// <para>M.Simple.Helper帮助类</para>
        /// <para>获取公钥/私钥</para>
        /// <para>每次生成的公钥和私钥都不一样</para>
        /// </summary>
        /// <returns>Encrypt   Decrypt</returns>
        public static RSAKeyModel GetKeyPair()
        {
            RSACryptoServiceProvider RSA = new();

            //包括专用参数： 如果为true，则包含公钥和私钥RSA密钥；false，仅包括公众钥匙 
            string publicKey = RSA.ToXmlString(false); //生成的是公开的解密key
            string privateKey = RSA.ToXmlString(true); //生成的是私有的既可以加密也可以解密的key
            return new RSAKeyModel(publicKey, privateKey);
        }
        /// <summary>
        /// <para>M.Simple.Helper帮助类</para>
        /// <para>RSA加密</para>
        /// </summary>
        /// <param name="encryptText">密文</param>
        /// <param name="encryptKey">秘钥</param>
        /// <returns>明文</returns>
        public static string RSAEncrypt(string encryptText, string encryptKey)
        {
            RSACryptoServiceProvider rsa = new();
            rsa.FromXmlString(encryptKey);

            //UnicodeEncoding ByteConverter = new UnicodeEncoding();
            //内容转成Unicode编码
            byte[] DataToEncrypt = Encoding.UTF8.GetBytes(encryptText);
            //生成Rsa码
            byte[] resultBytes = rsa.Encrypt(DataToEncrypt, false);

            //字节转字符串
            return Convert.ToBase64String(resultBytes);
        }
        /// <summary>
        /// <para>M.Simple.Helper帮助类</para>
        /// <para>RSA解密</para>
        /// </summary>
        /// <param name="decryptText">密文</param>
        /// <param name="decryptKey">秘钥</param>
        /// <returns>明文</returns>
        public static string RSADecrypt(string decryptText, string decryptKey)
        {
            //字符转字节
            byte[] dataToDecrypt = Convert.FromBase64String(decryptText);

            RSACryptoServiceProvider RSA = new();
            RSA.FromXmlString(decryptKey);
            //解码
            byte[] resultBytes = RSA.Decrypt(dataToDecrypt, false);

            //UnicodeEncoding ByteConverter = new UnicodeEncoding();
            //根据Unicode转化成字符串
            return Encoding.UTF8.GetString(resultBytes);
        }
        /// <summary>
        /// <para>M.Simple.Helper帮助类</para>
        /// <para>生成签名</para>
        /// </summary>
        /// <param name="content">原文</param>
        /// <param name="privatekey">私钥</param>
        /// <returns>密文</returns>
        public static string RSASignData(string content, string privatekey)
        {
            //字符转字节
            byte[] messagebytes = Encoding.UTF8.GetBytes(content);

            RSACryptoServiceProvider oRSA3 = new();
            oRSA3.FromXmlString(privatekey);

            //生成签名
            byte[] AOutput = oRSA3.SignData(messagebytes, "SHA1");
            //字节转字符
            return Convert.ToBase64String(AOutput);
        }
        /// <summary>
        /// <para>M.Simple.Helper帮助类</para>
        ///<para> 验证签名</para>
        /// </summary>
        /// <param name="content">原文</param>
        /// <param name="autograph">签名</param>
        /// <param name="publickey">共有key</param>
        /// <returns>验证成功：true  验证失败：false</returns>
        public static bool RSAVerifyData(string content, string autograph, string publickey)
        {
            //字符转字节
            byte[] messagebytes = Encoding.UTF8.GetBytes(content);
            byte[] messageAutographbytes = Convert.FromBase64String(autograph);

            RSACryptoServiceProvider RSA = new();
            RSA.FromXmlString(publickey);

            //验证 content 和 autograph 是否相同
            bool bVerify = RSA.VerifyData(messagebytes, "SHA1", messageAutographbytes);
            return bVerify;
        }
        /// <summary>
        /// <para>M.Simple.Helper帮助类</para>
        /// <para>RSA加密（字符过长，分段加密）</para>
        /// </summary>
        /// <param name="content">明文</param>
        /// <param name="publicKey">秘钥</param>
        /// <returns>密文</returns>
        public static string RSAEncryptWithBlock(string content, string publicKey)
        {
            using RSACryptoServiceProvider RSACryptography = new();
            RSACryptography.FromXmlString(publicKey);
            byte[] PlaintextData = Encoding.UTF8.GetBytes(content);
            int MaxBlockSize = RSACryptography.KeySize / 8 - 11;    //加密块最大长度限制

            if (PlaintextData.Length <= MaxBlockSize)
                return Convert.ToBase64String(RSACryptography.Encrypt(PlaintextData, false));

            using MemoryStream PlaiStream = new(PlaintextData);
            using MemoryStream CrypStream = new();
            byte[] Buffer = new byte[MaxBlockSize];
            int BlockSize = PlaiStream.Read(Buffer, 0, MaxBlockSize);

            while (BlockSize > 0)
            {
                byte[] ToEncrypt = new byte[BlockSize];
                Array.Copy(Buffer, 0, ToEncrypt, 0, BlockSize);

                byte[] Cryptograph = RSACryptography.Encrypt(ToEncrypt, false);
                CrypStream.Write(Cryptograph, 0, Cryptograph.Length);

                BlockSize = PlaiStream.Read(Buffer, 0, MaxBlockSize);
            }

            return Convert.ToBase64String(CrypStream.ToArray(), Base64FormattingOptions.None);
        }
        /// <summary>
        /// <para>M.Simple.Helper帮助类</para>
        /// <para>RSA解密</para>
        /// </summary>
        /// <param name="content"></param>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        public static string RSADecryptWithBlock(string content, string privateKey)
        {
            using RSACryptoServiceProvider RSACryptography = new();
            RSACryptography.FromXmlString(privateKey);
            byte[] CiphertextData = Convert.FromBase64String(content);
            int MaxBlockSize = RSACryptography.KeySize / 8;    //解密块最大长度限制 117

            if (CiphertextData.Length <= MaxBlockSize)
                return Encoding.UTF8.GetString(RSACryptography.Decrypt(CiphertextData, false));

            using MemoryStream CrypStream = new(CiphertextData);
            using MemoryStream PlaiStream = new();
            byte[] Buffer = new byte[MaxBlockSize];
            int BlockSize = CrypStream.Read(Buffer, 0, MaxBlockSize);

            while (BlockSize > 0)
            {
                byte[] ToDecrypt = new byte[BlockSize];
                Array.Copy(Buffer, 0, ToDecrypt, 0, BlockSize);

                byte[] Plaintext = RSACryptography.Decrypt(ToDecrypt, false);
                PlaiStream.Write(Plaintext, 0, Plaintext.Length);

                BlockSize = CrypStream.Read(Buffer, 0, MaxBlockSize);
            }

            return Encoding.UTF8.GetString(PlaiStream.ToArray());
        }
    }

    internal class MD5EncryptHelper
    {
        public static MD5EncryptResultModel MD5Encrypt64(string password)
        {
            string encryptSecret = CreateKey();
            string cl =string.Concat(password, encryptSecret);//组合
            //string pwd = "";
            MD5 md5 = MD5.Create(); //实例化一个md5对像
            // 加密后是一个字节类型的数组，这里要注意编码UTF8/Unicode等的选择　
            byte[] s = md5.ComputeHash(Encoding.UTF8.GetBytes(cl));
            return new MD5EncryptResultModel {  CipherText=Convert.ToBase64String(s),EncryptSecret= encryptSecret };
        }
        public static string MD5Encrypt64(string password,string encryptSecret)
        {
            string cl = string.Concat(password, encryptSecret);//组合
            //string pwd = "";
            MD5 md5 = MD5.Create(); //实例化一个md5对像
            // 加密后是一个字节类型的数组，这里要注意编码UTF8/Unicode等的选择　
            byte[] s = md5.ComputeHash(Encoding.UTF8.GetBytes(cl));
            return Convert.ToBase64String(s);
        }
        public static bool MD5Verify(string password, string cipherText, string encryptSecret)
        {
            return MD5Encrypt64(password, encryptSecret).Equals(cipherText);
        }
        private static string CreateKey() 
        {
            var rd = new Random();
            var rand = RandomNumberGenerator.Create();
            byte[] bytesKey = new byte[rd.Next(0,128)];
            rand.GetBytes(bytesKey);

            byte[] datetimeBytes= Encoding.UTF8.GetBytes(DateTime.Now.ToString());
            var bytesConcat = bytesKey.Concat(datetimeBytes).ToArray();
            return Convert.ToBase64String(bytesConcat);
        }
    }
}
