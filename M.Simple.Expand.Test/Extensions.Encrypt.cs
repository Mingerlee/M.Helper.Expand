using M.Simple.Helper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M.Simple.Expand.Test
{
    [TestClass]
    public class ExtensionsEncrypt
    {
        [TestMethod]
        public void TestEncryptReturn()
        {
            string str = "中国";
            string str1 = "中国湖南省长沙市开福区";
            var s = str.AesEncrypt();
            var s1 = str1.AesEncrypt();
            Console.WriteLine(JsonConvert.SerializeObject(str.AesEncrypt()));
            Console.WriteLine(JsonConvert.SerializeObject(str1.AesEncrypt()));
            Console.WriteLine();

            Console.WriteLine(s.CipherText.AesDecrypt(s.Key, s.IV));
            Console.WriteLine(s1.CipherText.AesDecrypt(s1.Key, s1.IV));

        }
        [TestMethod]
        public void TestEncryptBytes()
        {
            string str = "中国";
            string str1 = "中国湖南省长沙市开福区";
            string key = "KtRVUlzck5nWpdxjHutse6OtYu87hu0=";
            string iv = "Jpc/ihIFsjQMXg==";

            byte[] decBytes = Encoding.UTF8.GetBytes(key);
            byte[] ivBytes = Encoding.UTF8.GetBytes(iv);

            string s = str.AesEncrypt(decBytes, ivBytes);
            string s1 = str1.AesEncrypt(decBytes, ivBytes);

            Console.WriteLine(s);
            Console.WriteLine(s1);
            Console.WriteLine();

            Console.WriteLine(s.AesDecrypt(decBytes, ivBytes));
            Console.WriteLine(s1.AesDecrypt(decBytes, ivBytes));

        }
        [TestMethod]
        public void TestEncrypt()
        {
            string str = "中国";
            string str1 = "中国湖南省长沙市开福区";
            string key = "snNoeYgdGlP+KtRVUlzck5nWpdxjHuts";
            string iv = "ptkDRViyJpc/ihIF";

            string s = str.AesEncrypt(key, iv);
            string s1 = str1.AesEncrypt(key, iv);

            Console.WriteLine(s);
            Console.WriteLine(s1);
            Console.WriteLine();

            Console.WriteLine(s.AesDecrypt(key, iv));
            Console.WriteLine(s1.AesDecrypt(key, iv));

        }
    }
}
