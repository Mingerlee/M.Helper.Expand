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
}
