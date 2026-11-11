using Microsoft.Extensions.Configuration;

namespace M.Simple.Expand
{
    /// <summary>
    /// <para>M.Simple.Expand扩展</para>    
    /// <para>  配置文件读取辅助类</para>    
    /// </summary>
    public class AppSetting
    {
        private static readonly object objLock = new();
        private static AppSetting? instance = null;
        /// <summary>
        /// 
        /// </summary>
        private IConfiguration Config { get; }
        /// <summary>
        /// 
        /// </summary>
        private AppSetting()
        {
            var builder = new ConfigurationBuilder()
                                .SetConfigurationBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFileWithBasePath("appsettings.json", optional: false, reloadOnChange: true);
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
                    instance ??= new AppSetting();
                }
            }

            return instance;
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>读取配置文件value</para>
        /// </summary>
        /// <param name="name">eg:ConnectionStrings:DefaultConnection</param>
        /// <returns></returns>
        public static string GetConfig(string name)
        {
            return GetInstance().Config.GetSection(name).Value;
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>Int32</para>
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static int GetConfigInt32(string name)
        {
            return Convert.ToInt32(GetInstance().Config.GetSection(name).Value);
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>bool</para>
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool GetConfigBoolean(string name)
        {
            return Convert.ToBoolean(GetInstance().Config.GetSection(name).Value);
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>节点是否存在</para>
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool ConfigExist(string name)
        {
            try
            {
                return GetInstance().Config.GetSection(name).Exists();
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>
        ///     <para>获取节点下所有内容</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T GetConfigSection<T>(string name) where T:class
        {
            var appSettingsSection = GetInstance().Config.GetSection(name);
            return appSettingsSection.Get<T>()!;
        }
    }
}
