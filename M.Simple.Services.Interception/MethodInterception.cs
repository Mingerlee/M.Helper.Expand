using System.Reflection;

namespace M.Simple.Services.Interception;

/// <summary>Marks an interface, class or method for interception when attributed-only mode is enabled.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, Inherited = true)]
public sealed class MethodHookAttribute : Attribute;

/// <summary>Skips hooks. Method rules override type rules; a method skip always wins.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, Inherited = true)]
public sealed class SkipMethodHookAttribute : Attribute;

/// <summary>Calls the named instance method on the service before the business method.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public sealed class BeforeMethodAttribute(string methodName) : Attribute
{
    public string MethodName { get; } = methodName;
    public Type? HookType { get; }
    public BeforeMethodAttribute(Type hookType, string methodName) : this(methodName)
        => HookType = hookType ?? throw new ArgumentNullException(nameof(hookType));
}

/// <summary>Calls a named instance method after successful business completion.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public sealed class AfterMethodAttribute(string methodName) : Attribute
{
    public string MethodName { get; } = methodName;
    public Type? HookType { get; }
    public AfterMethodAttribute(Type hookType, string methodName) : this(methodName)
        => HookType = hookType ?? throw new ArgumentNullException(nameof(hookType));
}

/// <summary>One invocation. Before hooks can replace arguments; successful after hooks can replace the result.</summary>
public sealed class MethodInvocationContext
{
    private readonly object?[] _arguments;
    private readonly ParameterInfo[] _parameters;
    private readonly Type _resultType;
    private InvocationPhase _phase = InvocationPhase.Before;
    internal MethodInvocationContext(object target, MethodInfo method, MethodInfo implementation, object?[] args)
    {
        Target = target;
        Method = method;
        ImplementationMethod = implementation;
        _arguments = args;
        _parameters = method.GetParameters();
        var returnType = method.ReturnType;
        _resultType = returnType.IsGenericType && (returnType.GetGenericTypeDefinition() == typeof(Task<>)
            || returnType.GetGenericTypeDefinition() == typeof(ValueTask<>)) ? returnType.GetGenericArguments()[0]
            : returnType == typeof(Task) || returnType == typeof(ValueTask) ? typeof(void) : returnType;
        Arguments = Array.AsReadOnly(args);
    }

    public object Target { get; }
    public MethodInfo Method { get; }
    public MethodInfo ImplementationMethod { get; }
    public IReadOnlyList<object?> Arguments { get; }
    /// <summary>First CancellationToken argument, if any.</summary>
    public CancellationToken CancellationToken => _arguments.OfType<CancellationToken>().FirstOrDefault();
    public object? Result { get; internal set; }
    public Exception? Exception { get; internal set; }
    public Exception? BeforeException { get; internal set; }
    public bool IsCanceled => Exception is OperationCanceledException;
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();

    /// <summary>Reads an argument by its interface parameter name, without conversion.</summary>
    public T? GetArgument<T>(string name) => (T?)_arguments[ArgumentIndex(name)];

    /// <summary>Replaces an argument during the before phase. No implicit type conversions are performed.</summary>
    public void SetArgument(string name, object? value)
    {
        if (_phase != InvocationPhase.Before) throw new InvalidOperationException("Arguments can only be replaced in before hooks.");
        var index = ArgumentIndex(name);
        ValidateValue(_parameters[index].ParameterType, value);
        _arguments[index] = value;
    }

    /// <summary>Replaces the unwrapped return value in successful after hooks; cannot suppress exceptions.</summary>
    public void SetResult(object? value)
    {
        if (_phase != InvocationPhase.After || Exception is not null || _resultType == typeof(void))
            throw new InvalidOperationException("A result can only be replaced in after hooks following a successful value-returning invocation.");
        ValidateValue(_resultType, value);
        Result = value;
    }

    private int ArgumentIndex(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var index = Array.FindIndex(_parameters, p => p.Name == name);
        if (index < 0) throw new ArgumentException($"Unknown interface parameter: {name}.", nameof(name));
        return index;
    }

    private static void ValidateValue(Type type, object? value)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        if (value is null ? type.IsValueType && underlying is null : !(underlying ?? type).IsInstanceOfType(value))
            throw new ArgumentException($"Value must be assignable to {type}; no automatic conversion is performed.", nameof(value));
    }

    internal void BeginBusiness() => _phase = InvocationPhase.Business;
    internal void BeginAfter() => _phase = InvocationPhase.After;
    internal void Finish() => _phase = InvocationPhase.Completed;
    private enum InvocationPhase { Before, Business, After, Completed }
}

/// <summary>Use sync hooks for sync methods; async hooks default to their sync equivalents.</summary>
public abstract class MethodInterceptor
{
    public virtual void Before(MethodInvocationContext context) { }
    public virtual void After(MethodInvocationContext context) { }
    public virtual ValueTask BeforeAsync(MethodInvocationContext context)
    {
        Before(context);
        return ValueTask.CompletedTask;
    }
    public virtual ValueTask AfterAsync(MethodInvocationContext context)
    {
        After(context);
        return ValueTask.CompletedTask;
    }
}
