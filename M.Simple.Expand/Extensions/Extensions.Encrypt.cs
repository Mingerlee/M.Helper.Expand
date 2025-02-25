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
    }
}
