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
}

/// <summary>Calls the named instance method on the service after business completion, failure or cancellation.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public sealed class AfterMethodAttribute(string methodName) : Attribute
{
    public string MethodName { get; } = methodName;
}

/// <summary>One invocation, shared between before and after hooks. Arguments and result are not rewritten by the proxy.</summary>
public sealed class MethodInvocationContext
{
    internal MethodInvocationContext(object target, MethodInfo method, MethodInfo implementation, object?[] args)
    {
        Target = target;
        Method = method;
        ImplementationMethod = implementation;
        Arguments = Array.AsReadOnly(args);
        CancellationToken = args.OfType<CancellationToken>().FirstOrDefault();
    }

    public object Target { get; }
    public MethodInfo Method { get; }
    public MethodInfo ImplementationMethod { get; }
    public IReadOnlyList<object?> Arguments { get; }
    /// <summary>First CancellationToken argument, if any. After hooks must still run when it is cancelled.</summary>
    public CancellationToken CancellationToken { get; }
    public object? Result { get; internal set; }
    public Exception? Exception { get; internal set; }
    public bool IsCanceled => Exception is OperationCanceledException;
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();
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
