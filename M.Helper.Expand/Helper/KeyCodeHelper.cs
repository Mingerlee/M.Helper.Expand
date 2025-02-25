using System;
using System.Threading;

namespace M.Helper.Expand
{
    /// <summary>
    ///     主键编码帮助
    /// </summary>
    public class KeyCodeHelper
    {
        private static object objLock = new object();
        private const long Send = 1576492637564;//初始种子

        //时间戳41位+序列10位+机器码10位+随机3位

        private static int IndexBits = 10;//序列位
        private static int WorkerBits = 10;//机器位
        private static int RandomBits = 3;//随机位

        private static long Index = 0;
        private static long Worker = 6;

        private static long IndexMax = -1 ^ (-1 << IndexBits);
        //private static long WorkerMax = -1 ^ (-1 << WorkerBits);
        //private static long RandomMax = -1 ^ (-1 << RandomBits);

        // 机器码偏左移13位
        private static int WorkerShift = RandomBits;
        // 序列位偏左移3位
        private static int IndexShift = WorkerBits + RandomBits;
        // 时间毫秒左移23位
        private static int TimeStampShift = IndexBits + WorkerBits + RandomBits;

        //private static Random r = new Random(Guid.NewGuid().GetHashCode());

        private static long LastTimeStamp = 0;

        /// <summary>
        /// 生成一个ID
        /// </summary>
        /// <returns></returns>
        public static string GetSnowCode()
        {
            lock (objLock)
            {
                long ts = GetTimeStamp();
                if (ts == LastTimeStamp)
                {
                    Index = (Index + 1) & IndexMax;
                    if (Index == 0) ts = NextTimeStamp();
                }
                else
                {
                    Index = 0;
                }

                if (ts < LastTimeStamp)
                {
                    Thread.Sleep((int)(LastTimeStamp - ts));
                    //throw new Exception("NTP 回拨时间");
                }
                LastTimeStamp = ts;
                //long random = r.Next(0, (int)RandomMax);

                return $"{DateTime.Now:yyyyMMdd}" + (((ts - Send) << TimeStampShift) | (Index << IndexShift) | (Worker << WorkerShift)).ToString();
            }
        }
        /// <summary>
        /// 阻塞到下一个毫秒，直到获得新的时间
        /// </summary>
        /// <returns></returns>
        private static long NextTimeStamp()
        {
            long ts = GetTimeStamp();
            while (ts <= LastTimeStamp)
            {
                ts = GetTimeStamp();
            }
            return ts;
        }

        private static DateTime UTC0 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        /// <summary>
        /// 获取当前时间
        /// </summary>
        /// <returns></returns>
        private static long GetTimeStamp()
        {
            return (long)(DateTime.UtcNow - UTC0).TotalMilliseconds;
        }
    }
}
