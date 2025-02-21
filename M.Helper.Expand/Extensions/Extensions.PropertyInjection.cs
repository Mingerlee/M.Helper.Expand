using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace M.Helper.Expand
{
    /// <summary>
    ///     注册应用程序域中所有有AppService特性的服务
    ///     使用该属性注入的时候需要注意，Program(Startup)配置中使用  builder.Services.AddAppServices();//注册应用服务
    ///     Autofac容器注入时，需要添加.PropertiesAutowired() 允许属性注入
    /// </summary>
    public static class AppServiceExtensions
    {
        /// <summary>
        /// 注册应用程序域中所有有AppService特性的服务
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
    }
    /// <summary>
    /// 从容器装配service
    /// </summary>
    [AppService]
    public class AutowiredService
    {
        IServiceProvider serviceProvider;
        /// <summary>
        ///     
        /// </summary>
        /// <param name="serviceProvider"></param>
        public AutowiredService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        Dictionary<Type, Action<object, IServiceProvider>> autowiredActions = new Dictionary<Type, Action<object, IServiceProvider>>();
        /// <summary>
        ///     
        /// </summary>
        /// <param name="service"></param>
        public void Autowired(object service)
        {
            Autowired(service, serviceProvider);
        }
        /// <summary>
        /// 装配属性和字段
        /// </summary>
        /// <param name="service"></param>
        /// <param name="serviceProvider"></param>
        public void Autowired(object service, IServiceProvider serviceProvider)
        {
            var serviceType = service.GetType();
            if (autowiredActions.TryGetValue(serviceType, out Action<object, IServiceProvider> act))
            {
                act(service, serviceProvider);
            }
            else
            {
                //参数
                var objParam = Expression.Parameter(typeof(object), "obj");
                var spParam = Expression.Parameter(typeof(IServiceProvider), "sp");

                var obj = Expression.Convert(objParam, serviceType);
                var GetService = typeof(IServiceProvider).GetMethod("GetService");
                List<Expression> setList = new List<Expression>();

                //字段赋值
                foreach (FieldInfo field in serviceType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    var autowiredAttr = field.GetCustomAttribute<AutowiredAttribute>();
                    if (autowiredAttr != null)
                    {
                        var fieldExp = Expression.Field(obj, field);
                        var createService = Expression.Call(spParam, GetService, Expression.Constant(field.FieldType));
                        var setExp = Expression.Assign(fieldExp, Expression.Convert(createService, field.FieldType));
                        setList.Add(setExp);
                    }
                }
                //属性赋值
                foreach (PropertyInfo property in serviceType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    var autowiredAttr = property.GetCustomAttribute<AutowiredAttribute>();
                    if (autowiredAttr != null)
                    {
                        var propExp = Expression.Property(obj, property);
                        var createService = Expression.Call(spParam, GetService, Expression.Constant(property.PropertyType));
                        var setExp = Expression.Assign(propExp, Expression.Convert(createService, property.PropertyType));
                        setList.Add(setExp);
                    }
                }
                var bodyExp = Expression.Block(setList);
                var setAction = Expression.Lambda<Action<object, IServiceProvider>>(bodyExp, objParam, spParam).Compile();
                autowiredActions[serviceType] = setAction;
                setAction(service, serviceProvider);
            }
        }
    }
    /// <summary>
    ///     AppService特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class AppServiceAttribute : Attribute
    {
        /// <summary>
        /// 生命周期
        /// </summary>
        public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Singleton;
        /// <summary>
        /// 指定服务类型
        /// </summary>
        public Type? ServiceType { get; set; }
        /// <summary>
        /// 是否可以从第一个接口获取服务类型
        /// </summary>
        public bool InterfaceServiceType { get; set; } = true;
    }
    /// <summary>
    ///     属性注入--自动注入特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class AutowiredAttribute : Attribute
    {
    }
}
