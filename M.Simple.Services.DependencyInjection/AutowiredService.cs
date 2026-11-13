using M.Simple.Services.DependencyInjection;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace M.Simple.Services.DependencyInjection
{
    /// <summary>
    /// <para>M.Simple.Services服务类</para>
    /// <para>从容器装配service</para>
    /// </summary>
    /// <remarks>
    ///     
    /// </remarks>
    /// <param name="serviceProvider"></param>
    [AppService(Lifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped, InterfaceServiceType = false)]
    public class AutowiredService(IServiceProvider serviceProvider)
    {
        private readonly IServiceProvider serviceProvider = serviceProvider;
        private readonly ConcurrentDictionary<Type, Lazy<Action<object, IServiceProvider>>> autowiredActions = new();
        /// <summary>
        ///     
        /// </summary>
        /// <param name="service"></param>
        public void Autowired(object service)
        {
            Autowired(service, serviceProvider);
        }
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        /// <para>装配属性和字段</para>
        /// </summary>
        /// <param name="service"></param>
        /// <param name="serviceProvider"></param>
        public void Autowired(object service, IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(service);
            ArgumentNullException.ThrowIfNull(serviceProvider);
            // 缓存装配委托而非依赖实例；同一类型只编译一次，调用时使用当前provider。
            var action = autowiredActions.GetOrAdd(service.GetType(), static serviceType =>
                new Lazy<Action<object, IServiceProvider>>(() => CreateAutowiredAction(serviceType),
                    LazyThreadSafetyMode.ExecutionAndPublication)).Value;
            action(service, serviceProvider);
        }

        private static Action<object, IServiceProvider> CreateAutowiredAction(Type serviceType)
        {
            //参数
            var objParam = Expression.Parameter(typeof(object), "obj");
            var spParam = Expression.Parameter(typeof(IServiceProvider), "sp");

            var obj = Expression.Convert(objParam, serviceType);
            var GetService = typeof(IServiceProvider).GetMethod("GetService");
            List<Expression> setList = new List<Expression>();

            if (GetService == null) throw new ArgumentNullException("GetService is null...");

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
            return setAction;
        }
    }
}
