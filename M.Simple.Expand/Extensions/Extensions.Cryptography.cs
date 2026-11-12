using System.Security.Cryptography;
using System.Text;
using M.Simple.Expand.Models;

namespace M.Simple.Expand
{
    public static partial class Extensions
    {
        /// <summary>计算UTF-8文本的SHA-256摘要，返回Base64；空串计算标准空消息摘要。</summary>
        /// <param name="text">原文，不能为null</param>
        /// <returns>Base64编码的32字节摘要</returns>
        public static string Sha256Hash(this string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
        }

        /// <summary>历史MD5摘要别名，结果与MD5Encrypt完全一致，仅供旧协议兼容。</summary>
        /// <param name="text">原文；null和空串沿用历史空结果</param>
        /// <returns>历史Base64摘要或空串；摘要不可解密</returns>
        public static string Md5Hash(this string text) => text.MD5Encrypt();

        /// <summary>历史加盐MD5摘要别名，保留原有盐生成与结果模型。</summary>
        /// <param name="text">原文</param>
        /// <returns>历史摘要和盐；仅供旧协议兼容</returns>
        public static MD5EncryptResultModel Md5SaltHash(this string text) => text.MD5SaltEncrypt();

        /// <summary>历史加盐MD5摘要别名，按原文拼接盐计算，结果与MD5SaltEncrypt一致。</summary>
        /// <param name="text">原文</param>
        /// <param name="salt">历史盐值</param>
        /// <returns>Base64摘要；仅供旧协议兼容</returns>
        public static string Md5SaltHash(this string text, string salt) => text.MD5SaltEncrypt(salt);

        /// <summary>使用RSA和SHA-256签名UTF-8原文，默认PSS，返回Base64签名。</summary>
        /// <param name="text">原文，不能为null</param>
        /// <param name="privateKey">XML或PEM私钥；至少2048位</param>
        /// <param name="padding">默认PSS；需对接PKCS#1 v1.5时显式传入Pkcs1</param>
        /// <returns>Base64签名，不包含算法或密钥元数据</returns>
        public static string RsaSignDataSha256(this string text, string privateKey, RSASignaturePadding? padding = null)
        {
            ArgumentNullException.ThrowIfNull(text);
            using RSA rsa = ImportSigningKey(privateKey);
            return Convert.ToBase64String(rsa.SignData(Encoding.UTF8.GetBytes(text), HashAlgorithmName.SHA256,
                padding ?? RSASignaturePadding.Pss));
        }

        /// <summary>使用RSA和SHA-256验签，不自动尝试其他摘要或填充算法。</summary>
        /// <param name="text">UTF-8原文</param>
        /// <param name="signature">Base64签名</param>
        /// <param name="publicKey">XML或PEM公钥；至少2048位</param>
        /// <param name="padding">默认PSS，必须与签名方一致</param>
        /// <returns>签名不匹配返回false；无效Base64、密钥或参数抛异常</returns>
        public static bool RsaVerifyDataSha256(this string text, string signature, string publicKey, RSASignaturePadding? padding = null)
        {
            ArgumentNullException.ThrowIfNull(text);
            ArgumentNullException.ThrowIfNull(signature);
            using RSA rsa = ImportSigningKey(publicKey);
            return rsa.VerifyData(Encoding.UTF8.GetBytes(text), Convert.FromBase64String(signature),
                HashAlgorithmName.SHA256, padding ?? RSASignaturePadding.Pss);
        }

        private static RSA ImportSigningKey(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            RSA rsa = RSA.Create();
            try
            {
                // 复用历史XML密钥，也支持标准PEM；新入口不放宽密钥强度要求。
                if (key.TrimStart().StartsWith("<", StringComparison.Ordinal)) rsa.FromXmlString(key);
                else rsa.ImportFromPem(key);
                if (rsa.KeySize < 2048) throw new CryptographicException("新签名接口要求RSA密钥至少2048位。");
                return rsa;
            }
            catch
            {
                rsa.Dispose();
                throw;
            }
        }
    }
}
