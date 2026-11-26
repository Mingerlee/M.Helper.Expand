using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace M.Simple.Services.Interception;

internal sealed class EmptyMethodInterceptor : MethodInterceptor;

internal sealed record BoundHook(object Target, MethodInfo Method);
internal sealed record NamedMethodPlan(BoundHook? Before, BoundHook? After);

internal sealed class NamedMethodInterceptor(object target, MethodInterceptor inner,
    IReadOnlyDictionary<MethodInfo, NamedMethodPlan> plans, ILogger? logger) : MethodInterceptor
{
    internal static NamedMethodPlan CreatePlan(object target, MethodInfo contract, MethodInfo implementation, IServiceProvider? provider)
    {
        if (contract.IsDefined(typeof(SkipMethodHookAttribute), true) || implementation.IsDefined(typeof(SkipMethodHookAttribute), true))
            return new(null, null);
        // Implementation attributes override interface attributes independently for each phase.
        var before = implementation.GetCustomAttribute<BeforeMethodAttribute>(true) ?? contract.GetCustomAttribute<BeforeMethodAttribute>(true);
        var after = implementation.GetCustomAttribute<AfterMethodAttribute>(true) ?? contract.GetCustomAttribute<AfterMethodAttribute>(true);
        var asyncBusiness = IsAsyncReturn(contract.ReturnType);
        return new(before is null ? null : Bind(target, before.HookType, before.MethodName, implementation, asyncBusiness, provider),
            after is null ? null : Bind(target, after.HookType, after.MethodName, implementation, asyncBusiness, provider));
    }

    private static BoundHook Bind(object target, Type? hookType, string name, MethodInfo business,
        bool asyncBusiness, IServiceProvider? provider)
    {
        var method = Find(hookType ?? target.GetType(), name, business, asyncBusiness);
        if (hookType is null) return new(target, method);
        if (!method.IsPublic) throw new ArgumentException($"External hook {hookType.FullName}.{name} must be public.");
        if (provider is null)
            throw new InvalidOperationException($"External hook {hookType.FullName} requires an IServiceProvider. Register the hook and pass serviceProvider to Create or use DI registration.");
        return new(provider.GetRequiredService(hookType), method);
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

    private object? InvokeHook(BoundHook? hook, MethodInvocationContext context)
    {
        if (hook is null) return null;
        try { return hook.Method.Invoke(hook.Target, [context]); }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private async ValueTask InvokeHookAsync(BoundHook? hook, MethodInvocationContext context)
    {
        var result = InvokeHook(hook, context);
        if (hook?.Method.ReturnType == typeof(Task)) await ((Task)result!).ConfigureAwait(false);
        else if (hook?.Method.ReturnType == typeof(ValueTask)) await ((ValueTask)result!).ConfigureAwait(false);
    }

    private void Log(Exception error, MethodInvocationContext context, bool before)
    {
        try
        {
            if (logger is not null)
                logger.LogError(error, "{Phase} hook failed for {Service}.{Method}", before ? "Before" : "After", target.GetType().FullName, context.Method.Name);
            else
                System.Diagnostics.Trace.TraceError("Hook failed for {0}.{1}: {2}", target.GetType().FullName, context.Method.Name, error);
        }
        catch { } // A logging provider must not replace the business outcome.
    }

    private void Run(Action action, MethodInvocationContext context, bool before)
    {
        var result = context.Result;
        try { action(); }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested) { throw; }
        catch (Exception error)
        {
            if (before) context.BeforeException = error;
            else context.Result = result;
            Log(error, context, before);
        }
    }

    private async ValueTask RunAsync(Func<ValueTask> action, MethodInvocationContext context, bool before)
    {
        var result = context.Result;
        try { await action().ConfigureAwait(false); }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested) { throw; }
        catch (Exception error)
        {
            if (before) context.BeforeException = error;
            else context.Result = result;
            Log(error, context, before);
        }
    }

    public override void Before(MethodInvocationContext context)
    {
        Run(() => inner.Before(context), context, true);
        Run(() => InvokeHook(Plan(context).Before, context), context, true);
    }

    public override async ValueTask BeforeAsync(MethodInvocationContext context)
    {
        await RunAsync(() => inner.BeforeAsync(context), context, true).ConfigureAwait(false);
        await RunAsync(() => InvokeHookAsync(Plan(context).Before, context), context, true).ConfigureAwait(false);
    }

    public override void After(MethodInvocationContext context)
    {
        Run(() => InvokeHook(Plan(context).After, context), context, false);
        Run(() => inner.After(context), context, false);
    }

    public override async ValueTask AfterAsync(MethodInvocationContext context)
    {
        await RunAsync(() => InvokeHookAsync(Plan(context).After, context), context, false).ConfigureAwait(false);
        await RunAsync(() => inner.AfterAsync(context), context, false).ConfigureAwait(false);
    }
}
