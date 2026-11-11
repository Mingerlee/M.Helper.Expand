namespace M.Simple.Expand
{
    /// <summary>
    ///     雪花编码帮助
    /// </summary>
    public class SnowCodeGenerator
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
        /// 生成雪花编码(包含时间戳)
        /// </summary>
        /// <returns></returns>
        public static string GetTimeStampSnowCode()
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
        /// 生成雪花编码
        /// </summary>
        /// <returns></returns>
        public static long GetSnowCode()
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

                return (((ts - Send) << TimeStampShift) | (Index << IndexShift) | (Worker << WorkerShift));
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
    /// <summary>
    ///     
    /// </summary>
    public sealed class SnowflakeIdGenerator
    {
        // 起始时间戳（2026-03-05），可自定义
        private const long Epoch = 1741000000000L;

        // 机器ID位数（10位，支持1024台机器）
        private const int WorkerIdBits = 10;
        // 序列号位数（12位，每毫秒4096个）
        private const int SequenceBits = 12;

        // 最大机器ID（1023）
        private const long MaxWorkerId = (1 << WorkerIdBits) - 1;
        // 最大序列号（4095）
        private const long MaxSequence = (1 << SequenceBits) - 1;

        // 机器ID左移位数
        private const int WorkerIdShift = SequenceBits;
        // 时间戳左移位数
        private const int TimestampShift = SequenceBits + WorkerIdBits;

        // 私有变量（线程安全）
        private long _lastTimestamp = -1L;
        private long _sequence = 0L;
        private readonly long _workerId;
        private readonly object _lockObj = new object();

        /// <summary>
        /// 初始化雪花算法生成器
        /// </summary>
        /// <param name="workerId">机器ID（0-1023），分布式环境需保证唯一</param>
        /// <exception cref="ArgumentException">机器ID超出范围</exception>
        public SnowflakeIdGenerator(long workerId)
        {
            if (workerId < 0 || workerId > MaxWorkerId)
            {
                throw new ArgumentException($"机器ID必须在0-{MaxWorkerId}之间", nameof(workerId));
            }
            _workerId = workerId;
        }

        /// <summary>
        /// 生成雪花ID（64位长整型）
        /// 性能：单机每秒可生成数百万个ID，线程安全
        /// </summary>
        /// <returns>唯一雪花ID</returns>
        /// <exception cref="InvalidOperationException">时钟回拨异常</exception>
        public long NextId()
        {
            lock (_lockObj)
            {
                // 高性能获取当前时间戳（毫秒）
                long currentTimestamp = GetCurrentTimestamp();

                // 处理时钟回拨（健壮性保障）
                if (currentTimestamp < _lastTimestamp)
                {
                    // 容忍5ms内的回拨，超过则抛出异常
                    long diff = _lastTimestamp - currentTimestamp;
                    if (diff <= 5)
                    {
                        Thread.SpinWait(100); // 自旋等待，避免线程切换
                        currentTimestamp = GetCurrentTimestamp();
                        if (currentTimestamp < _lastTimestamp)
                        {
                            throw new InvalidOperationException($"时钟回拨异常：{diff}ms");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"时钟回拨异常：{diff}ms");
                    }
                }

                // 同一毫秒内，序列号自增
                if (currentTimestamp == _lastTimestamp)
                {
                    _sequence = (_sequence + 1) & MaxSequence;
                    // 序列号溢出，等待下一毫秒
                    if (_sequence == 0)
                    {
                        currentTimestamp = WaitNextMillis(_lastTimestamp);
                    }
                }
                else
                {
                    // 新毫秒，重置序列号
                    _sequence = 0;
                }

                _lastTimestamp = currentTimestamp;

                // 组合最终ID
                return ((currentTimestamp - Epoch) << TimestampShift)
                       | (_workerId << WorkerIdShift)
                       | _sequence;
            }
        }

        /// <summary>
        /// 转换雪花ID为字符串（可选）
        /// </summary>
        /// <returns>字符串格式的雪花ID</returns>
        public string NextIdString()
        {
            return NextId().ToString();
        }

        #region 私有辅助方法
        /// <summary>
        /// 获取当前时间戳（毫秒）
        /// </summary>
        private long GetCurrentTimestamp()
        {
            // 高性能时间戳获取（比DateTime.UtcNow快）
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// 等待下一毫秒
        /// </summary>
        private long WaitNextMillis(long lastTimestamp)
        {
            long timestamp = GetCurrentTimestamp();
            while (timestamp <= lastTimestamp)
            {
                Thread.SpinWait(10); // 自旋等待，最小化开销
                timestamp = GetCurrentTimestamp();
            }
            return timestamp;
        }
        #endregion
    }
    /// <summary>
    ///     唯一编码帮助
    /// </summary>
    public static class UniqueCodeGenerator
    {
        // 缓存字符数组，避免重复分配内存
        private static readonly char[] _hexChars = "0123456789ABCDEF".ToCharArray();

        /// <summary>
        /// 生成高性能唯一编码（Guid优化版）
        /// 格式：32位大写无分隔符（如：550E8400E29B41D4A716446655440000）
        /// 性能：单机每秒可生成百万级编码
        /// </summary>
        /// <returns>32位唯一编码</returns>
        public static string GenerateGuidCode()
        {
            // 获取Guid的字节数组（避免ToString的字符串拼接开销）
            Span<byte> guidBytes = stackalloc byte[16];
            Guid.NewGuid().TryWriteBytes(guidBytes);

            // 预分配字符数组（栈分配，无GC）
            Span<char> result = stackalloc char[32];

            // 手动转换字节到16进制（比ToString("N")快30%+）
            for (int i = 0; i < 16; i++)
            {
                result[i * 2] = _hexChars[guidBytes[i] >> 4];
                result[i * 2 + 1] = _hexChars[guidBytes[i] & 0x0F];
            }

            return new string(result);
        }

        /// <summary>
        /// 生成带时间戳的唯一编码（可排序）
        /// 格式：UTC时间自0001-01-01起的毫秒数 + 完整32位大写无分隔符GUID
        /// 时间戳仅提供毫秒级时间顺序，同一毫秒内不保证生成顺序；唯一性依赖完整GUID
        /// </summary>
        /// <returns>带时间戳的唯一编码</returns>
        public static string GenerateTimeStampGuidCode()
        {
            // 保留原有时间戳口径，不将其变更为Unix时间戳或日期格式。
            long timestamp = DateTime.UtcNow.Ticks / 10000;

            // 复用完整GUID编码，避免缓冲区不足导致写入失败及截断GUID带来的碰撞。
            return $"{timestamp}{GenerateGuidCode()}";
        }
    }
    /// <summary>
    ///     短编码帮助
    /// </summary>
    public static class ShortCodeGenerator
    {
        private const string _charset = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        private static readonly int _base = _charset.Length;

        // 进程内共享生成器，保留同毫秒内的序列状态；NextId内部加锁保护并发调用。
        private static readonly SnowflakeIdGenerator _snowflakeIdGenerator = new(1);

        /// <summary>
        /// 将长整型转换为Base62短编码
        /// </summary>
        /// <param name="number">雪花ID</param>
        /// <returns>Base62编码（11位）</returns>
        private static string Encode(long number)
        {
            if (number == 0) return _charset[0].ToString();

            var sb = new System.Text.StringBuilder();
            while (number > 0)
            {
                sb.Append(_charset[(int)(number % _base)]);
                number /= _base;
            }

            // 补位到11位，保证长度一致
            return string.Join("", sb.ToString().PadRight(11, _charset[0]).ToCharArray().Reverse().ToArray());
        }

        /// <summary>
        /// 生成Base62短编码（基于雪花ID）
        /// 进程内复用固定节点号1的生成器，不保证跨进程或进程重启后的唯一性
        /// </summary>
        /// <returns>11位短编码，同一进程内共享生成器的序列状态</returns>
        public static string GenerateShortCode()
        {
            return Encode(_snowflakeIdGenerator.NextId());
        }
    }
}
