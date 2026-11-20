using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace M.Simple.Services.Interception;

/// <summary>DispatchProxy implementation. Use Create or the DI registration extensions.</summary>
public class MethodHookProxy<TService> : DispatchProxy where TService : class
{
    /// <summary>When business and after-hook failures occur together, the after failure is attached to the business exception Data.</summary>
    public const string AfterExceptionDataKey = "M.Simple.Services.Interception.AfterException";
    private TService _target = default!;
    private MethodInterceptor _interceptor = default!;
    private bool _attributedOnly;

    /// <summary>Creates an attribute-driven proxy without requiring a separate interceptor.</summary>
    public static TService Create(TService target, bool attributedOnly = true)
        => Create(target, new EmptyMethodInterceptor(), attributedOnly);

    public static TService Create(TService target, MethodInterceptor interceptor, bool attributedOnly = false)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(interceptor);
        ValidateContract();
        // Validate implementation signatures before any business method can run.
        foreach (var method in ContractMethods())
        {
            var implementation = ResolveImplementation(target, method);
            if (implementation.IsDefined(typeof(AsyncStateMachineAttribute), false) && method.ReturnType == typeof(void))
                throw new NotSupportedException("async void methods cannot be intercepted. Return Task instead.");
        }
        var proxy = DispatchProxy.Create<TService, MethodHookProxy<TService>>();
        var handler = (MethodHookProxy<TService>)(object)proxy;
        handler._target = target;
        handler._interceptor = new NamedMethodInterceptor(target, interceptor,
            ContractMethods().ToDictionary(method => method, method =>
                NamedMethodInterceptor.CreatePlan(target, method, ResolveImplementation(target, method))));
        handler._attributedOnly = attributedOnly;
        return proxy;
    }

    private static IEnumerable<MethodInfo> ContractMethods() => typeof(TService).GetInterfaces()
        .Append(typeof(TService)).SelectMany(t => t.GetMethods()).Distinct();

    internal static void ValidateContract()
    {
        if (!typeof(TService).IsInterface)
            throw new ArgumentException("The service contract must be an interface.");
        if (typeof(IDisposable).IsAssignableFrom(typeof(TService)) || typeof(IAsyncDisposable).IsAssignableFrom(typeof(TService)))
            throw new NotSupportedException("Keep disposal on the implementation; proxy interfaces must not inherit IDisposable/IAsyncDisposable.");
        foreach (var method in ContractMethods())
        {
            var result = method.ReturnType;
            if (method.IsStatic || result.IsByRef || result.IsByRefLike || result.IsPointer
                || method.GetParameters().Any(p => p.ParameterType.IsByRef || p.ParameterType.IsByRefLike || p.ParameterType.IsPointer)
                || IsAsyncStream(result)
                || (result.GetMethods(BindingFlags.Public | BindingFlags.Instance).Any(m => m.Name == "GetAwaiter" && m.GetParameters().Length == 0)
                    && !IsStandardAwaitable(result))
                || (typeof(Task).IsAssignableFrom(result) && result != typeof(Task)
                    && (!result.IsGenericType || result.GetGenericTypeDefinition() != typeof(Task<>))))
                throw new NotSupportedException($"Unsupported interception signature: {method}.");
        }
    }

    private static bool IsStandardAwaitable(Type type) => type == typeof(Task) || type == typeof(ValueTask)
        || type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Task<>) || type.GetGenericTypeDefinition() == typeof(ValueTask<>));

    private static bool IsAsyncStream(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>)
        || type.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>));

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(targetMethod);
        var implementation = ResolveImplementation(_target, targetMethod);
        if (!ShouldIntercept(targetMethod, implementation)) return InvokeTarget(targetMethod, args);
        var context = new MethodInvocationContext(_target, targetMethod, implementation, args ?? []);
        var type = targetMethod.ReturnType;
        if (type == typeof(Task)) return RunTask(context, args);
        if (type == typeof(ValueTask)) return new ValueTask(RunTask(context, args));
        if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Task<>) || type.GetGenericTypeDefinition() == typeof(ValueTask<>)))
        {
            var adapter = type.GetGenericTypeDefinition() == typeof(Task<>) ? nameof(RunTaskResult) : nameof(RunValueTaskResult);
            return typeof(MethodHookProxy<TService>).GetMethod(adapter, BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(type.GetGenericArguments()[0]).Invoke(this, [context, args]);
        }
        return RunSync(context, args);
    }

    private bool ShouldIntercept(MethodInfo method, MethodInfo implementation)
    {
        if (method.IsDefined(typeof(SkipMethodHookAttribute), true) || implementation.IsDefined(typeof(SkipMethodHookAttribute), true)) return false;
        if (method.IsDefined(typeof(MethodHookAttribute), true) || implementation.IsDefined(typeof(MethodHookAttribute), true)) return true;
        if (method.IsDefined(typeof(BeforeMethodAttribute), true) || implementation.IsDefined(typeof(BeforeMethodAttribute), true)
            || method.IsDefined(typeof(AfterMethodAttribute), true) || implementation.IsDefined(typeof(AfterMethodAttribute), true)) return true;
        var types = typeof(TService).GetInterfaces().Append(typeof(TService)).Append(_target.GetType());
        if (types.Any(t => t.IsDefined(typeof(SkipMethodHookAttribute), true))) return false;
        return !_attributedOnly || types.Any(t => t.IsDefined(typeof(MethodHookAttribute), true));
    }

    private static MethodInfo ResolveImplementation(TService target, MethodInfo method)
    {
        var map = target.GetType().GetInterfaceMap(method.DeclaringType!);
        var definition = method.IsGenericMethod ? method.GetGenericMethodDefinition() : method;
        var index = Array.IndexOf(map.InterfaceMethods, definition);
        if (index < 0) throw new MissingMethodException(method.DeclaringType?.FullName, method.Name);
        var implementation = map.TargetMethods[index];
        return method.IsGenericMethod && !method.IsGenericMethodDefinition
            ? implementation.MakeGenericMethod(method.GetGenericArguments()) : implementation;
    }

    private object? InvokeTarget(MethodInfo method, object?[]? args)
    {
        try { return method.Invoke(_target, args); }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private object? RunSync(MethodInvocationContext context, object?[]? args)
    {
        Before(context);
        try { context.Result = InvokeTarget(context.Method, args); }
        catch (Exception error) { context.Exception = error; throw; }
        finally { Complete(context); }
        return context.Result;
    }

    private void Complete(MethodInvocationContext context)
    {
        context.BeginAfter();
        try { _interceptor.After(context); }
        catch (Exception error) when (context.Exception is not null) { AttachAfterFailure(context.Exception, error); }
        finally { context.Finish(); }
    }

    private async ValueTask CompleteAsync(MethodInvocationContext context)
    {
        context.BeginAfter();
        try { await _interceptor.AfterAsync(context).ConfigureAwait(false); }
        catch (Exception error) when (context.Exception is not null) { AttachAfterFailure(context.Exception, error); }
        finally { context.Finish(); }
    }

    private void Before(MethodInvocationContext context)
    {
        try { _interceptor.Before(context); context.BeginBusiness(); }
        catch { context.Finish(); throw; }
    }

    private async ValueTask BeforeAsync(MethodInvocationContext context)
    {
        try { await _interceptor.BeforeAsync(context).ConfigureAwait(false); context.BeginBusiness(); }
        catch { context.Finish(); throw; }
    }

    private static void AttachAfterFailure(Exception businessError, Exception afterError)
    {
        // Even exceptions with read-only Data must not let the after-hook failure replace the business error.
        try { businessError.Data[AfterExceptionDataKey] = afterError; } catch { }
    }

    private async Task RunTask(MethodInvocationContext context, object?[]? args)
    {
        await BeforeAsync(context).ConfigureAwait(false);
        try
        {
            var result = InvokeTarget(context.Method, args);
            if (context.Method.ReturnType == typeof(ValueTask)) await ((ValueTask)result!).ConfigureAwait(false);
            else await ((Task)result!).ConfigureAwait(false);
        }
        catch (Exception error) { context.Exception = error; throw; }
        finally { await CompleteAsync(context).ConfigureAwait(false); }
    }

    private async Task<TResult> RunTaskResult<TResult>(MethodInvocationContext context, object?[]? args)
    {
        await BeforeAsync(context).ConfigureAwait(false);
        try
        {
            var result = await ((Task<TResult>)InvokeTarget(context.Method, args)!).ConfigureAwait(false);
            context.Result = result;
        }
        catch (Exception error) { context.Exception = error; throw; }
        finally { await CompleteAsync(context).ConfigureAwait(false); }
        return (TResult)context.Result!;
    }

    private async ValueTask<TResult> RunValueTaskResult<TResult>(MethodInvocationContext context, object?[]? args)
    {
        await BeforeAsync(context).ConfigureAwait(false);
        try
        {
            var result = await ((ValueTask<TResult>)InvokeTarget(context.Method, args)!).ConfigureAwait(false);
            context.Result = result;
        }
        catch (Exception error) { context.Exception = error; throw; }
        finally { await CompleteAsync(context).ConfigureAwait(false); }
        return (TResult)context.Result!;
    }
}
