using System.Reflection;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace M.Simple.Expand;

public static partial class Extensions
{
    private const BindingFlags DefaultReflectionFlags = BindingFlags.Public | BindingFlags.Instance;

    /// <summary>按名称获取公开实例属性；找不到时返回 null。</summary>
    public static PropertyInfo? GetPropertyOrNull(this Type type, string name, BindingFlags flags = DefaultReflectionFlags)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return type.GetProperty(name, flags);
    }

    /// <summary>按名称获取公开实例字段；找不到时返回 null。</summary>
    public static FieldInfo? GetFieldOrNull(this Type type, string name, BindingFlags flags = DefaultReflectionFlags)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return type.GetField(name, flags);
    }

    /// <summary>按名称和参数类型精确获取方法，避免重载造成歧义。</summary>
    public static MethodInfo? GetMethodOrNull(this Type type, string name, Type[] parameterTypes,
        BindingFlags flags = DefaultReflectionFlags)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(parameterTypes);
        return type.GetMethod(name, flags | BindingFlags.ExactBinding, binder: null, types: parameterTypes, modifiers: null);
    }

    /// <summary>按名称读取公开实例属性；不存在或不可读时抛出 MissingMemberException。</summary>
    public static T? GetPropertyValue<T>(this object source, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(source);
        var property = source.GetType().GetPropertyOrNull(propertyName);
        if (property?.GetMethod?.IsPublic != true || property.GetIndexParameters().Length != 0)
            throw new MissingMemberException(source.GetType().FullName, propertyName);
        var value = property.GetValue(source);
        // 不把 null 隐式转换为 0/false，以免丢失数据的真实状态。
        return (T?)value;
    }

    /// <summary>按名称设置公开实例属性；字符串值沿用 SetStringValue 的类型转换规则。</summary>
    public static void SetPropertyValue(this object target, string propertyName, object? value)
    {
        ArgumentNullException.ThrowIfNull(target);
        var property = target.GetType().GetPropertyOrNull(propertyName);
        if (property?.SetMethod?.IsPublic != true || property.GetIndexParameters().Length != 0)
            throw new MissingMemberException(target.GetType().FullName, propertyName);

        if (value is string text && !property.PropertyType.IsInstanceOfType(value))
        {
            property.SetStringValue(target, text);
            return;
        }

        var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        if (value is null && property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) is null)
            throw new ArgumentException("不能将 null 赋给非可空值类型属性。", nameof(value));
        if (value is not null && !targetType.IsInstanceOfType(value))
            throw new ArgumentException($"值类型 {value.GetType()} 与属性类型 {property.PropertyType} 不兼容。", nameof(value));
        property.SetValue(target, value);
    }

    /// <summary>判断类型是否为常见标量类型；可空类型按其底层类型判断。</summary>
    public static bool IsSimpleType(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        type = Nullable.GetUnderlyingType(type) ?? type;
        return (type.IsPrimitive && type != typeof(void)) || type.IsEnum || type == typeof(string) || type == typeof(decimal)
            || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan)
            || type == typeof(Guid) || type == typeof(DateOnly) || type == typeof(TimeOnly);
    }

    /// <summary>判断是否为 Nullable&lt;T&gt;；无法判断引用类型的可空注解。</summary>
    public static bool IsNullableType(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return Nullable.GetUnderlyingType(type) is not null;
    }

    /// <summary>Nullable&lt;T&gt; 返回 T，其余类型返回自身。</summary>
    public static Type GetNonNullableType(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return Nullable.GetUnderlyingType(type) ?? type;
    }

    /// <summary>读取单个特性；无特性时返回 null，重复特性仍由框架报告歧义。</summary>
    public static TAttribute? GetAttribute<TAttribute>(this MemberInfo member, bool inherit = true) where TAttribute : Attribute
    {
        ArgumentNullException.ThrowIfNull(member);
        return member.GetCustomAttribute<TAttribute>(inherit);
    }

    /// <summary>读取特性列表；继承和 AllowMultiple 规则沿用框架行为。</summary>
    public static IReadOnlyList<TAttribute> GetAttributes<TAttribute>(this MemberInfo member, bool inherit = true) where TAttribute : Attribute
    {
        ArgumentNullException.ThrowIfNull(member);
        return member.GetCustomAttributes<TAttribute>(inherit).ToArray();
    }

    /// <summary>是否为公开可读的非静态、非索引属性。</summary>
    public static bool IsPublicReadable(this PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(property);
        return property.GetMethod is { IsPublic: true, IsStatic: false } && property.GetIndexParameters().Length == 0;
    }

    /// <summary>是否为公开可写的非静态、非索引属性；不表示业务上允许修改。</summary>
    public static bool IsPublicWritable(this PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(property);
        return property.SetMethod is { IsPublic: true, IsStatic: false } && property.GetIndexParameters().Length == 0;
    }

    /// <summary>获取 Display.Name、DisplayName、Description，均未提供时返回成员名称。</summary>
    public static string GetMemberDisplayName(this MemberInfo member)
    {
        ArgumentNullException.ThrowIfNull(member);
        return member.GetAttribute<DisplayAttribute>()?.GetName()
            ?? member.GetAttribute<DisplayNameAttribute>()?.DisplayName
            ?? member.GetAttribute<DescriptionAttribute>()?.Description ?? member.Name;
    }

    /// <summary>将公开可读实例属性浅拷贝到字典。默认保留 null、使用属性名称；显示名称冲突会报错。</summary>
    public static Dictionary<string, object?> ToPropertyDictionary(this object source, bool ignoreNull = false,
        bool useDisplayNames = false, IEnumerable<string>? excludedProperties = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        var excluded = new HashSet<string>(excludedProperties ?? [], StringComparer.Ordinal);
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in source.GetType().GetProperties(DefaultReflectionFlags))
        {
            if (!property.IsPublicReadable() || excluded.Contains(property.Name)) continue;
            var value = property.GetValue(source);
            if (ignoreNull && value is null) continue;
            result.Add(useDisplayNames ? property.GetMemberDisplayName() : property.Name, value);
        }
        return result;
    }

    /// <summary>返回类型自身或其基类中匹配泛型定义的构造类型；找不到时返回 null。</summary>
    public static Type? GetGenericBaseType(this Type type, Type genericDefinition)
    {
        ArgumentNullException.ThrowIfNull(type);
        ValidateGenericDefinition(genericDefinition);
        for (Type? current = type; current is not null; current = current.BaseType)
            if (current.IsGenericType && current.GetGenericTypeDefinition() == genericDefinition) return current;
        return null;
    }

    /// <summary>返回匹配泛型接口定义的全部接口（包括自身），保留同一接口定义的不同构造类型。</summary>
    public static IReadOnlyList<Type> GetGenericInterfaces(this Type type, Type genericInterfaceDefinition)
    {
        ArgumentNullException.ThrowIfNull(type);
        ValidateGenericDefinition(genericInterfaceDefinition);
        if (!genericInterfaceDefinition.IsInterface)
            throw new ArgumentException("必须传入泛型接口定义。", nameof(genericInterfaceDefinition));
        return type.GetInterfaces().Append(type).Where(t => t.IsInterface && t.IsGenericType
            && t.GetGenericTypeDefinition() == genericInterfaceDefinition).Distinct().ToArray();
    }

    private static void ValidateGenericDefinition(Type definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (!definition.IsGenericTypeDefinition)
            throw new ArgumentException("必须传入开放泛型定义，例如 typeof(IEnumerable<>)。", nameof(definition));
    }

    /// <summary>获取程序集成功加载的类型；部分类型加载失败时保留其余可用类型。</summary>
    public static IReadOnlyList<Type> GetLoadableTypes(this Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException exception) { return exception.Types.OfType<Type>().ToArray(); }
    }

    /// <summary>查找程序集中的公开、非抽象且非开放泛型的接口实现类型。</summary>
    public static IReadOnlyList<Type> GetImplementations<TContract>(this Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var contract = typeof(TContract);
        return assembly.GetLoadableTypes()
            .Where(type => type.IsClass && type.IsVisible && !type.IsAbstract && !type.ContainsGenericParameters
                && contract.IsAssignableFrom(type))
            .ToArray();
    }
}
