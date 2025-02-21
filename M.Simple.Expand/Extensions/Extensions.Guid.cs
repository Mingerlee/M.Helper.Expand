using System;

namespace M.Simple.Expand
{
    public static partial class Extensions
    {
        /// <summary>
        ///<para>M.Simple.Expand扩展</para>
        ///<para>获取Guid</para>
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static Guid GetNextGuid(this Guid guid)
        {
            byte[] b = Guid.NewGuid().ToByteArray();
            DateTime dateTime = new(1900, 1, 1);
            DateTime now = DateTime.Now;
            TimeSpan timeSpan = new(now.Ticks - dateTime.Ticks);
            TimeSpan timeOfDay = now.TimeOfDay;
            byte[] bytes1 = BitConverter.GetBytes(timeSpan.Days);
            byte[] bytes2 = BitConverter.GetBytes((long)(timeOfDay.TotalMilliseconds / 3.333333));
            Array.Reverse(bytes1);
            Array.Reverse(bytes2);
            Array.Copy(bytes1, bytes1.Length - 2, b, b.Length - 6, 2);
            Array.Copy(bytes2, bytes2.Length - 4, b, b.Length - 4, 4);
            return new Guid(b);
        }
        /// <summary>
        ///<para>M.Simple.Expand扩展</para>
        ///<para>随机秘钥(Guid)</para>
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static string GetEncryptSecret(this Guid guid)
        {
            return guid.GetNextGuid().ToString().Replace("-", "").PadLeft(32).ToUpper();
        }
    }
}
