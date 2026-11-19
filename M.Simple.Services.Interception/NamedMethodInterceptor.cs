using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace M.Simple.Services.Interception;

internal sealed class EmptyMethodInterceptor : MethodInterceptor;

internal sealed record NamedMethodPlan(MethodInfo? Before, MethodInfo? After);

internal sealed class NamedMethodInterceptor(object target, MethodInterceptor inner,
    IReadOnlyDictionary<MethodInfo, NamedMethodPlan> plans) : MethodInterceptor
{
    internal static NamedMethodPlan CreatePlan(object target, MethodInfo contract, MethodInfo implementation)
    {
        if (contract.IsDefined(typeof(SkipMethodHookAttribute), true) || implementation.IsDefined(typeof(SkipMethodHookAttribute), true))
            return new(null, null);
        // Implementation attributes override interface attributes independently for each phase.
        var before = implementation.GetCustomAttribute<BeforeMethodAttribute>(true) ?? contract.GetCustomAttribute<BeforeMethodAttribute>(true);
        var after = implementation.GetCustomAttribute<AfterMethodAttribute>(true) ?? contract.GetCustomAttribute<AfterMethodAttribute>(true);
        var asyncBusiness = IsAsyncReturn(contract.ReturnType);
        return new(before is null ? null : Find(target.GetType(), before.MethodName, implementation, asyncBusiness),
            after is null ? null : Find(target.GetType(), after.MethodName, implementation, asyncBusiness));
    }

    private NamedMethodPlan Plan(MethodInvocationContext context)
        => plans[context.Method.IsGenericMethod ? context.Method.GetGenericMethodDefinition() : context.Method];

    private static bool IsAsyncReturn(Type type) => type == typeof(Task) || type == typeof(ValueTask)
        || type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Task<>) || type.GetGenericTypeDefinition() == typeof(ValueTask<>));

    private static MethodInfo Find(Type type, string name, MethodInfo business, bool asyncBusiness)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Hook method name must not be blank.");
        for (Type? current = type; current is not null; current = current.BaseType)
        {
            var candidates = current.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
                | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Where(m => m.Name == name).ToArray();
            if (candidates.Length == 0) continue;
            var matches = candidates.Where(m => !m.IsStatic && !m.IsGenericMethod
                && m.GetParameters() is [{ ParameterType: var parameter }] && parameter == typeof(MethodInvocationContext)).ToArray();
            if (matches.Length != 1)
                throw new ArgumentException($"Hook {type.FullName}.{name} must have exactly one instance overload taking MethodInvocationContext.");
            var hook = matches[0];
            if (hook == business || (business.IsGenericMethod && hook == business.GetGenericMethodDefinition()))
                throw new ArgumentException("A business method cannot be its own hook.");
            if (hook.ReturnType != typeof(void) && hook.ReturnType != typeof(Task) && hook.ReturnType != typeof(ValueTask))
                throw new ArgumentException($"Hook {name} must return void, Task or ValueTask.");
            if (hook.ReturnType == typeof(void) && hook.IsDefined(typeof(AsyncStateMachineAttribute), false))
                throw new ArgumentException($"Hook {name} must not be async void.");
            if (!asyncBusiness && hook.ReturnType != typeof(void))
                throw new ArgumentException($"Synchronous method {business.Name} requires a synchronous void hook: {name}.");
            return hook;
        }
        throw new MissingMethodException(type.FullName, name);
    }

    private object? InvokeHook(MethodInfo? hook, MethodInvocationContext context)
    {
        if (hook is null) return null;
        try { return hook.Invoke(target, [context]); }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private async ValueTask InvokeHookAsync(MethodInfo? hook, MethodInvocationContext context)
    {
        var result = InvokeHook(hook, context);
        if (hook?.ReturnType == typeof(Task)) await ((Task)result!).ConfigureAwait(false);
        else if (hook?.ReturnType == typeof(ValueTask)) await ((ValueTask)result!).ConfigureAwait(false);
    }

    public override void Before(MethodInvocationContext context)
    {
        inner.Before(context);
        InvokeHook(Plan(context).Before, context);
    }

    public override async ValueTask BeforeAsync(MethodInvocationContext context)
    {
        await inner.BeforeAsync(context).ConfigureAwait(false);
        await InvokeHookAsync(Plan(context).Before, context).ConfigureAwait(false);
    }

    public override void After(MethodInvocationContext context)
    {
        Exception? namedFailure = null;
        try { InvokeHook(Plan(context).After, context); }
        catch (Exception error) { namedFailure = error; }
        try { inner.After(context); }
        catch (Exception error) when (namedFailure is not null) { throw new AggregateException(namedFailure, error); }
        if (namedFailure is not null) ExceptionDispatchInfo.Capture(namedFailure).Throw();
    }

    public override async ValueTask AfterAsync(MethodInvocationContext context)
    {
        Exception? namedFailure = null;
        try { await InvokeHookAsync(Plan(context).After, context).ConfigureAwait(false); }
        catch (Exception error) { namedFailure = error; }
        try { await inner.AfterAsync(context).ConfigureAwait(false); }
        catch (Exception error) when (namedFailure is not null) { throw new AggregateException(namedFailure, error); }
        if (namedFailure is not null) ExceptionDispatchInfo.Capture(namedFailure).Throw();
    }
}
