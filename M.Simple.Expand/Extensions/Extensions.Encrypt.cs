using M.Simple.Expand.Models;
using M.Simple.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M.Simple.Expand
{
    public static partial class Extensions
    {
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>AES加密</para>
        /// </summary>
        /// <param name="plainText">需要加密的明文</param>
        /// <param name="key">秘钥</param>
        /// <param name="iv">初始化向量</param>
        /// <returns></returns>
        public static string AesEncrypt(this string plainText,string key,string iv)
        {
            AESEncryptHelper encryptHelper = new(key, iv);
            return encryptHelper.AesEncrypt(plainText);
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>AES加密</para>
        /// </summary>
        /// <param name="plainText">需要加密的明文</param>
        /// <param name="key">秘钥</param>
        /// <param name="iv">初始化向量</param>
        /// <returns></returns>
        public static string AesEncrypt(this string plainText, byte[] key, byte[] iv)
        {
            return AESEncryptHelper.AesEncrypt(plainText, key, iv);
        }
        
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>AES加密</para>
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns></returns>
        public static AESEncryptResultModel AesEncrypt(this string plainText)
        {
            return AESEncryptHelper.AesEncryptReturnKey(plainText);
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>AES解密</para>
        /// </summary>
        /// <param name="cipherText">加密后的密文</param>
        /// <param name="key">秘钥</param>
        /// <param name="iv">初始化向量</param>
        /// <returns></returns>
        public static string AesDecrypt(this string cipherText, string key, string iv)
        {
            AESEncryptHelper encryptHelper = new(key, iv);
            return encryptHelper.AesDecrypt(cipherText);
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>AES解密</para>
        /// </summary>
        /// <param name="cipherText">加密后的密文</param>
        /// <param name="key">秘钥</param>
        /// <param name="iv">初始化向量</param>
        /// <returns></returns>
        public static string AesDecrypt(this string cipherText, byte[] key, byte[] iv)
        {
            return AESEncryptHelper.AesDecrypt(cipherText, key, iv);
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>RSA加密</para>
        /// </summary>
        /// <param name="plainText">明文</param>
        /// <returns>返回：密文，公钥，私钥</returns>
        public static RSAEncryptResultModel RsaEncrypt(this string plainText)
        {
            var pair = RSAEncryptHelper.GetKeyPair();
            var cipherText = RSAEncryptHelper.RSAEncrypt(plainText, pair.PrivateKey);
            return new() { CipherText = cipherText, PublicKey = pair.PublicKey, PrivateKey = pair.PrivateKey };
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>RSA加密</para>
        /// </summary>
        /// <param name="plainText">明文</param>
        /// <param name="encryptKey">秘钥</param>
        /// <returns>密文</returns>
        public static string RsaEncrypt(this string plainText,string encryptKey)
        {
            return RSAEncryptHelper.RSAEncrypt(plainText, encryptKey);
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>RSA加密(分段)</para>
        /// </summary>
        /// <param name="plainText">明文</param>
        /// <param name="encryptKey">秘钥</param>
        /// <returns>密文</returns>
        public static string RsaEncryptWithBlock(this string plainText, string encryptKey)
        {
            return RSAEncryptHelper.RSAEncryptWithBlock(plainText, encryptKey);
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>RSA加密(分段)</para>
        /// </summary>
        /// <param name="plainText">明文</param>
        /// <returns>返回：密文，公钥，私钥</returns>
        public static RSAEncryptResultModel RsaEncryptWithBlock(this string plainText)
        {
            var pair = RSAEncryptHelper.GetKeyPair();
            var cipherText = RSAEncryptHelper.RSAEncryptWithBlock(plainText, pair.PrivateKey);
            return new() { CipherText = cipherText, PublicKey = pair.PublicKey, PrivateKey = pair.PrivateKey };
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>RSA生成签名</para>
        /// </summary>
        /// <param name="plainText">明文</param>
        /// <param name="encryptKey">秘钥</param>
        /// <returns>签名密文</returns>
        public static string RsaSignData(this string plainText, string encryptKey)
        {
            return RSAEncryptHelper.RSASignData(plainText, encryptKey);
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>RSA生成签名</para>
        /// </summary>
        /// <param name="plainText">明文</param>
        /// <param name="autograph">签名密文</param>
        /// <param name="publickey">公钥</param>
        /// <returns>签名密文</returns>
        public static bool RsaVerifyData(this string plainText, string autograph, string publickey)
        {
            return RSAEncryptHelper.RSAVerifyData(plainText, autograph,publickey);
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>RSA解密</para>
        /// </summary>
        /// <param name="cipherText">密文</param>
        /// <param name="privateKey">私钥</param>
        /// <returns>明文</returns>
        public static string RsaDecrypt(this string cipherText, string privateKey)
        {
            return RSAEncryptHelper.RSADecrypt(cipherText, privateKey);
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>RSA解密(分段)</para>
        /// </summary>
        /// <param name="cipherText">密文</param>
        /// <param name="privateKey">私钥</param>
        /// <returns>明文</returns>
        public static string RsaDecryptWithBlock(this string cipherText, string privateKey)
        {
            return RSAEncryptHelper.RSADecryptWithBlock(cipherText, privateKey);
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>MD5加密</para>
        /// </summary>
        /// <param name="plainText">明文</param>
        /// <returns>密文</returns>
        public static MD5EncryptResultModel MD5Encrypt(this string plainText)
        {
            return MD5EncryptHelper.MD5Encrypt64(plainText);
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>MD5验证</para>
        /// </summary>
        /// <param name="plainText">明文</param>
        /// <param name="cipherText">密文</param>
        /// <param name="encryptSecret">秘钥</param>
        /// <returns>bool</returns>
        public static bool MD5Verify(this string plainText, string cipherText, string encryptSecret)
        {
            return MD5EncryptHelper.MD5Verify(plainText, cipherText, encryptSecret);
        }
        
    }
}
