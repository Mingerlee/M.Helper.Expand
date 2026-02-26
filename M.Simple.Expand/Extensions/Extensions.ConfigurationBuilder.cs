using Microsoft.Extensions.Configuration;
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
        /// <para>自定义扩展方法，模拟 SetBasePath 的功能</para>    
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="basePath"></param>
        /// <returns></returns>
        public static IConfigurationBuilder SetConfigurationBasePath(this IConfigurationBuilder builder, string basePath)
        {
            // 核心逻辑：将基础路径存入 ConfigurationBuilder 的 Properties 字典（原 SetBasePath 的底层逻辑）
            builder.Properties["BasePath"] = basePath;
            return builder;
        }
        /// <summary>
        ///     <para>M.Simple.Expand扩展</para>    
        ///     <para>自定义 AddJsonFile 重载，自动使用已设置的基础路径</para>    
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="path"></param>
        /// <param name="optional"></param>
        /// <param name="reloadOnChange"></param>
        /// <returns></returns>
        public static IConfigurationBuilder AddJsonFileWithBasePath(this IConfigurationBuilder builder, string path, bool optional = false, bool reloadOnChange = false)
        {
            if (builder.Properties.TryGetValue("BasePath", out var basePathObj) && basePathObj is string basePath)
            {
                path = Path.Combine(basePath, path);
            }
            return builder.AddJsonFile(path, optional, reloadOnChange);
        }
    }
}
