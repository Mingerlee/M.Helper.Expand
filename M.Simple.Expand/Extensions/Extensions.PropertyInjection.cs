using M.Simple.Expand.Attributes;
using M.Simple.Expand.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace M.Simple.Expand
{
    public static partial class Extensions
    {
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>    
        /// <para>
        /// 注册应用程序域中所有有AppService特性的服务
        /// 使用该属性注入的时候需要注意，Program(Startup)配置中使用  builder.Services.AddAppServices();//注册应用服务
        /// Autofac容器注入时，需要添加.PropertiesAutowired() 允许属性注入
        ///</para>    
        /// </summary>
        /// <param name="services"></param>
        public static void AddAppServices(this IServiceCollection services)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    var serviceAttribute = type.GetCustomAttribute<AppServiceAttribute>();

                    if (serviceAttribute != null)
                    {
                        var serviceType = serviceAttribute.ServiceType;
                        if (serviceType == null && serviceAttribute.InterfaceServiceType)
                        {
                            serviceType = type.GetInterfaces().FirstOrDefault();
                        }
                        if (serviceType == null)
                        {
                            serviceType = type;
                        }
                        switch (serviceAttribute.Lifetime)
                        {
                            case ServiceLifetime.Singleton:
                                services.AddSingleton(serviceType, type);
                                break;
                            case ServiceLifetime.Scoped:
                                services.AddScoped(serviceType, type);
                                break;
                            case ServiceLifetime.Transient:
                                services.AddTransient(serviceType, type);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }
        /// <summary>
        ///     注入邮件配置
        /// </summary>
        /// <param name="services"></param>
        /// <param name="mailSettingsFunc">邮件服务器配置</param>
        /// <returns></returns>
        public static void AddEMailSetup(this IServiceCollection services, Func<IServiceProvider, MailSettings> mailSettingsFunc)
        {
            services.AddSingleton(mailSettingsFunc.Invoke);
            services.AddSingleton<IEMailService, EMailService>();
        }
        /// <summary>
        ///     注入ftp配置
        /// </summary>
        /// <param name="services"></param>
        /// <param name="ftpClientSettingsFunc">ftp配置</param>
        /// <returns></returns>
        public static void AddFtpClientSetup(this IServiceCollection services, Func<IServiceProvider, FtpClientSettings> ftpClientSettingsFunc)
        {
            services.AddSingleton(ftpClientSettingsFunc.Invoke);
            // 当前目录属于一次工作流；每次解析独立实例，避免不同调用共享可变目录。
            services.AddTransient<IFtpClientService, FtpClientService>();
        }
    }
}
