using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M.Simple.Expand.Models
{
    /// <summary>
    ///     AES加密结果模型
    /// </summary>
    public class AESEncryptResultModel
    {
        /// <summary>
        ///     密文
        /// </summary>
        public string CipherText { get; set; }
        /// <summary>
        ///     秘钥
        /// </summary>
        public byte[] Key { get; set; }
        /// <summary>
        ///     初始化向量
        /// </summary>
        public byte[] IV { get; set; }
    }
    /// <summary>
    /// 密钥类
    /// </summary>
    /// <remarks>
    /// 构造函数
    /// </remarks>
    /// <param name="publicKey">共有key</param>
    /// <param name="privateKey">私有key</param>
    public class RSAKeyModel(string publicKey, string privateKey)
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
    ///     AES加密结果模型
    /// </summary>
    public class RSAEncryptResultModel
    {
        /// <summary>
        ///     密文
        /// </summary>
        public string CipherText { get; set; }
        /// <summary>
        ///     公钥
        /// </summary>
        public string PublicKey { get; set; }
        /// <summary>
        ///     私钥
        /// </summary>
        public string PrivateKey { get; set; }
    }
    /// <summary>
    ///     MD5加密结果模型
    /// </summary>
    public class MD5EncryptResultModel
    {
        /// <summary>
        ///     密文
        /// </summary>
        public string CipherText { get; set; }
        /// <summary>
        ///     秘钥
        /// </summary>
        public string EncryptSecret { get; set; }
    }
}
