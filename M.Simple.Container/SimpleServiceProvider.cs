using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace M.Simple.Container;

/// <summary>A container using cached constructor plans and standard DI contracts.</summary>
public sealed class SimpleServiceProvider : IServiceProvider, IKeyedServiceProvider, IServiceScopeFactory,
    IServiceProviderIsService, IServiceProviderIsKeyedService, IDisposable, IAsyncDisposable
{
    private readonly Registration[] _registrations;
    private readonly Dictionary<ServiceId, Registration> _lastRegistrations;
    private static readonly Plan s_missingPlan = new(_ => null, false);
    private readonly ConcurrentDictionary<ServiceId, Plan> _plans = new();
    private readonly Dictionary<(Registration, Type), Plan> _registrationPlans = [];
    private readonly Dictionary<(object, Type), int> _slots = [];
    private readonly List<(Registration Registration, Type Service)> _building = [];
    private readonly object _planGate = new();
    private readonly ContainerScope _root;
    private readonly bool _validateScopes;
    [ThreadStatic] private static List<(SimpleServiceProvider Owner, Registration Registration, Type Service)>? s_factories;

    internal SimpleServiceProvider(IServiceCollection services, SimpleContainerOptions options)
    {
        _validateScopes = options.ValidateScopes;
        _registrations = services.Select(d => new Registration(d)).ToArray();
        _lastRegistrations = new Dictionary<ServiceId, Registration>(_registrations.Length);
        foreach (var registration in _registrations)
        {
            ValidateDescriptor(registration);
            _lastRegistrations[new ServiceId(registration.Service, registration.Key)] = registration;
        }
        _root = new ContainerScope(this, isRoot: true);
        if (options.ValidateOnBuild)
        {
            var errors = new List<Exception>();
            foreach (var registration in _registrations.Where(r => !r.Service.IsGenericTypeDefinition))
            {
                try { lock (_planGate) GetRegistrationPlan(registration, registration.Service); }
                catch (Exception ex) { errors.Add(new InvalidOperationException($"Invalid registration '{registration.Service}': {ex.Message}", ex)); }
            }
            if (errors.Count > 0) throw new AggregateException("Container validation failed.", errors);
        }
    }

    public object? GetService(Type serviceType) => _root.GetService(serviceType);
    public object? GetKeyedService(Type serviceType, object? serviceKey) => _root.GetKeyedService(serviceType, serviceKey);
    public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
        => GetKeyedService(serviceType, serviceKey) ?? throw Missing(serviceType, serviceKey);

    public bool IsService(Type serviceType) => IsKeyedService(serviceType, null);

    public bool IsKeyedService(Type serviceType, object? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        if (serviceType.ContainsGenericParameters) return false;
        if (serviceKey is null && IsBuiltIn(serviceType)) return true;
        if (IsEnumerable(serviceType)) return true;
        if (Equals(serviceKey, KeyedService.AnyKey)) return false;
        return FindRegistration(serviceType, serviceKey) is not null;
    }

    public IServiceScope CreateScope()
    {
        ThrowIfDisposed();
        return new ContainerScope(this, isRoot: false);
    }

    public AsyncServiceScope CreateAsyncScope() => new(CreateScope());

    public void Dispose() => _root.Dispose();
    public ValueTask DisposeAsync() => _root.DisposeAsync();

    internal void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_root.IsDisposed, this);

    internal object? Resolve(ContainerScope scope, Type serviceType, object? key)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        if (serviceType.ContainsGenericParameters) throw new ArgumentException($"Cannot resolve open type '{serviceType}'.", nameof(serviceType));
        if (key is null)
        {
            if (serviceType == typeof(IServiceProvider) || serviceType == typeof(IKeyedServiceProvider)) return scope.IsRoot ? this : scope;
            if (serviceType == typeof(IServiceScopeFactory) || serviceType == typeof(IServiceProviderIsService)
                || serviceType == typeof(IServiceProviderIsKeyedService)) return this;
        }
        if (Equals(key, KeyedService.AnyKey) && !IsEnumerable(serviceType))
            throw new InvalidOperationException("AnyKey can only be used to resolve IEnumerable<T>.");
        if (_plans.TryGetValue(new ServiceId(serviceType, key), out var cached)) return cached.Resolve(scope);
        Plan? plan;
        lock (_planGate) plan = GetPlan(serviceType, key);
        return plan?.Resolve(scope);
    }

    private Plan? GetPlan(Type service, object? key)
    {
        var id = new ServiceId(service, key);
        if (_plans.TryGetValue(id, out var cached)) return ReferenceEquals(cached, s_missingPlan) ? null : cached;
        if (key is null && IsBuiltIn(service))
            return new Plan(scope => Resolve(scope, service, null), false);

        var registration = FindRegistration(service, key);
        Plan? plan;
        if (registration is not null) plan = GetRegistrationPlan(registration, service);
        else if (IsEnumerable(service))
        {
            var itemType = service.GenericTypeArguments[0];
            var items = new List<Plan>();
            foreach (var candidate in _registrations)
            {
                var keyMatches = Equals(key, KeyedService.AnyKey) ? candidate.Key is not null : Equals(candidate.Key, key);
                if (!keyMatches || !Matches(candidate.Service, itemType)) continue;
                if (!CanClose(candidate, itemType)) continue; // Constraints can exclude an open registration from an enumerable.
                items.Add(GetRegistrationPlan(candidate, itemType));
            }
            plan = new Plan(EnumerableBinding.Bind(itemType, items.Select(p => p.Resolve).ToArray()), items.Any(p => p.ContainsScoped));
        }
        else plan = null;
        _plans[id] = plan ?? s_missingPlan;
        return plan;
    }

    private Registration? FindRegistration(Type service, object? key)
    {
        if (_lastRegistrations.TryGetValue(new ServiceId(service, key), out var registration)) return registration;
        return service.IsConstructedGenericType
               && _lastRegistrations.TryGetValue(new ServiceId(service.GetGenericTypeDefinition(), key), out registration)
            ? registration : null;
    }

    private Plan GetRegistrationPlan(Registration registration, Type service)
    {
        if (_registrationPlans.TryGetValue((registration, service), out var cached)) return cached;
        if (_building.Any(x => x.Registration == registration && x.Service == service))
            throw new InvalidOperationException($"Circular dependency: {Chain(service)}.");
        // A recursive generic graph can grow forever without repeating the exact closed type.
        if (_building.Count >= 128) throw new InvalidOperationException($"Dependency depth exceeds 128: {Chain(service)}.");
        _building.Add((registration, service));
        try
        {
            Func<ContainerScope, object?> create;
            var containsScoped = registration.Lifetime == ServiceLifetime.Scoped;
            var implementation = registration.Implementation;
            if (implementation?.IsGenericTypeDefinition == true)
            {
                try { implementation = implementation.MakeGenericType(service.GenericTypeArguments); }
                catch (ArgumentException ex) { throw new InvalidOperationException($"Cannot close '{implementation}' for '{service}'.", ex); }
                if (!service.IsAssignableFrom(implementation)) throw new InvalidOperationException($"'{implementation}' cannot implement '{service}'.");
            }
            if (registration.Instance is not null)
                create = _ => registration.Instance;
            else if (registration.Factory is not null)
                create = scope => InvokeFactory(registration, service, scope);
            else
            {
                var constructors = ConstructorBinding.GetConstructors(implementation!);
                if (constructors.Length == 0) throw new InvalidOperationException($"'{implementation}' has no public constructor. Dependency chain: {Chain(null)}.");
                CachedConstructor? selected = null;
                List<Func<ContainerScope, object?>>? arguments = null;
                HashSet<Type>? selectedParameters = null;
                var selectedScoped = false;
                var missing = new HashSet<string>();
                foreach (var constructor in constructors)
                {
                    var candidate = new List<Func<ContainerScope, object?>>();
                    var scoped = false;
                    foreach (var argument in constructor.Parameters)
                    {
                        if (argument.IsServiceKey)
                        {
                            if (registration.Key is null || !argument.Type.IsInstanceOfType(registration.Key))
                                throw new InvalidOperationException($"Service key cannot be assigned to '{argument.Name}' on '{implementation}'.");
                            var serviceKey = registration.Key;
                            candidate.Add(_ => serviceKey);
                            continue;
                        }
                        var key = argument.Key;
                        var dependency = GetPlan(argument.Type, key);
                        if (dependency is null)
                        {
                            if (argument.DefaultResolver is null)
                            {
                                missing.Add($"{argument.Type} (key: {key ?? "<none>"})");
                                break;
                            }
                            candidate.Add(argument.DefaultResolver);
                        }
                        else
                        {
                            scoped |= dependency.ContainsScoped;
                            candidate.Add(dependency.Resolve);
                        }
                    }
                    if (candidate.Count != constructor.Parameters.Length) continue;
                    if (selected is not null)
                    {
                        if (constructor.Parameters.Any(p => !selectedParameters!.Contains(p.Type)))
                            throw new InvalidOperationException($"Ambiguous constructors on '{implementation}'. Dependency chain: {Chain(null)}.");
                        continue;
                    }
                    selected = constructor;
                    arguments = candidate;
                    selectedScoped = scoped;
                    selectedParameters = constructor.Parameters.Select(p => p.Type).ToHashSet();
                }
                if (selected is null)
                    throw new InvalidOperationException($"No resolvable public constructor on '{implementation}'. Missing dependencies: {string.Join(", ", missing)}. Dependency chain: {Chain(null)}.");
                containsScoped |= selectedScoped;
                create = ConstructorBinding.Bind(selected.Invoker, arguments!.ToArray());
            }

            if (_validateScopes && registration.Lifetime == ServiceLifetime.Singleton && containsScoped)
                throw new InvalidOperationException($"Singleton '{service}' captures a Scoped service. Dependency chain: {Chain(null)}. Change the lifetime or create an explicit scope.");

            Func<ContainerScope, object?> resolve;
            if (registration.Instance is not null) resolve = create; // Caller owns externally supplied instances.
            else if (registration.Lifetime == ServiceLifetime.Transient)
                resolve = scope => scope.Capture(create(scope));
            else
            {
                var slotKey = (registration.Identity, implementation ?? service);
                if (!_slots.TryGetValue(slotKey, out var slot)) _slots[slotKey] = slot = _slots.Count;
                resolve = registration.Lifetime == ServiceLifetime.Singleton
                    ? _ => _root.GetOrCreate(slot, create)
                    : scope =>
                    {
                        if (_validateScopes && scope.IsRoot)
                            throw new InvalidOperationException($"Cannot resolve Scoped '{service}' from the root provider. Create a scope with CreateScope() or CreateAsyncScope().");
                        return scope.GetOrCreate(slot, create);
                    };
            }
            var plan = new Plan(resolve, containsScoped);
            _registrationPlans[(registration, service)] = plan;
            return plan;
        }
        finally { _building.RemoveAt(_building.Count - 1); }
    }

    private object? InvokeFactory(Registration registration, Type service, ContainerScope scope)
    {
        var stack = s_factories ??= [];
        if (stack.Any(x => x.Owner == this && x.Registration == registration && x.Service == service) || stack.Count >= 128)
            throw new InvalidOperationException($"Circular factory dependency: {string.Join(" -> ", stack.Select(x => x.Service.Name).Append(service.Name))}.");
        stack.Add((this, registration, service));
        try { return registration.Factory!(scope.IsRoot ? this : scope, registration.Key); }
        finally { stack.RemoveAt(stack.Count - 1); }
    }

    private string Chain(Type? last) => string.Join(" -> ", _building.Select(x => x.Service.ToString()).Concat(last is null ? [] : [last.ToString()]));
    private static bool Matches(Type registered, Type requested) => registered == requested
        || (requested.IsConstructedGenericType && registered == requested.GetGenericTypeDefinition());
    private static bool IsEnumerable(Type type) => type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
    private static bool IsBuiltIn(Type type) => type == typeof(IServiceProvider) || type == typeof(IKeyedServiceProvider)
        || type == typeof(IServiceScopeFactory) || type == typeof(IServiceProviderIsService) || type == typeof(IServiceProviderIsKeyedService);
    private static bool CanClose(Registration registration, Type service)
    {
        if (registration.Implementation?.IsGenericTypeDefinition != true) return true;
        try { return service.IsAssignableFrom(registration.Implementation.MakeGenericType(service.GenericTypeArguments)); }
        catch (ArgumentException) { return false; }
    }

    private static void ValidateDescriptor(Registration registration)
    {
        if (!Enum.IsDefined(registration.Lifetime)) throw new ArgumentException($"Invalid lifetime for '{registration.Service}'.");
        if (Equals(registration.Key, KeyedService.AnyKey))
            throw new NotSupportedException("Wildcard AnyKey registrations are not supported. Register explicit service keys instead.");
        if (registration.Service.IsGenericTypeDefinition)
        {
            if (registration.Implementation?.IsGenericTypeDefinition != true
                || registration.Implementation.GetGenericArguments().Length != registration.Service.GetGenericArguments().Length)
                throw new ArgumentException($"Open generic '{registration.Service}' requires an open implementation with matching arity.");
        }
        if (registration.Implementation is { } implementation)
        {
            if (implementation.IsAbstract || implementation.IsInterface || !implementation.IsClass
                || (implementation.ContainsGenericParameters && !implementation.IsGenericTypeDefinition))
                throw new ArgumentException($"'{implementation}' must be a concrete class or an open generic class.");
            if (!registration.Service.ContainsGenericParameters && !registration.Service.IsAssignableFrom(implementation))
                throw new ArgumentException($"'{implementation}' cannot implement '{registration.Service}'.");
        }
    }

    internal static InvalidOperationException Missing(Type type, object? key)
        => new($"No service registered for '{type}' (key: {key ?? "<none>"}).");

    private readonly record struct ServiceId(Type Type, object? Key);
    private sealed record Plan(Func<ContainerScope, object?> Resolve, bool ContainsScoped);

    private sealed class Registration
    {
        public Type Service { get; }
        public object? Key { get; }
        public Type? Implementation { get; }
        public object? Instance { get; }
        public Func<IServiceProvider, object?, object?>? Factory { get; }
        public ServiceLifetime Lifetime { get; }
        public object Identity { get; }

        public Registration(ServiceDescriptor descriptor)
        {
            Service = descriptor.ServiceType;
            Key = descriptor.ServiceKey;
            Lifetime = descriptor.Lifetime;
            Identity = descriptor is ScannedDescriptor scanned ? scanned.Identity : new object();
            if (descriptor.IsKeyedService)
            {
                Implementation = descriptor.KeyedImplementationType;
                Instance = descriptor.KeyedImplementationInstance;
                Factory = descriptor.KeyedImplementationFactory;
            }
            else
            {
                Implementation = descriptor.ImplementationType;
                Instance = descriptor.ImplementationInstance;
                if (descriptor.ImplementationFactory is { } factory) Factory = (provider, _) => factory(provider);
            }
        }
    }
}
