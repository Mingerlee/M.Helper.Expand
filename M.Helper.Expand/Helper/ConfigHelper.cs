using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace M.Helper.Expand
{
    /// <summary>
    /// 
    /// </summary>
    public class AppSetting
    {
        private static object objLock = new object();
        private static AppSetting instance = null;

        /// <summary>
        /// 
        /// </summary>
        private IConfigurationRoot Config { get; }

        /// <summary>
        /// 
        /// </summary>
        private AppSetting()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            Config = builder.Build();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static AppSetting GetInstance()
        {
            if (instance == null)
            {
                lock (objLock)
                {
                    if (instance == null)
                    {
                        instance = new AppSetting();
                    }
                }
            }

            return instance;
        }

        /// <summary>
        ///     string值
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetConfig(string name)
        {
            return name.IsNullOrEmptyString()?"":GetInstance().Config.GetSection(name).Value;
        }

        /// <summary>
        ///     int值
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static int GetConfigInt32(string name)
        {
            return Convert.ToInt32(GetInstance().Config.GetSection(name).Value);
        }
        /// <summary>
        ///     bool值
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool GetConfigBoolean(string name)
        {
            return Convert.ToBoolean(GetInstance().Config.GetSection(name).Value);
        }
        /// <summary>
        ///     节点是否存在
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool ConfigExist(string name)
        {
            try
            {
                var result = GetInstance().Config.GetSection(name).Value;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
