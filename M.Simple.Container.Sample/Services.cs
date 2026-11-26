namespace M.Simple.Container.Sample;

public interface IScopedService;
public interface IUserService : IScopedService
{
    Guid ScopeId { get; }
    User Get(int id);
}
public interface IUserAudit : IScopedService;
public interface IUserRepository : IScopedService
{
    User Get(int id);
}
public record User(int Id, string Name);

public sealed class UserService(IUserRepository repository) : IUserService, IUserAudit
{
    public Guid ScopeId { get; } = Guid.NewGuid();
    public User Get(int id) => repository.Get(id);
}

public sealed class UserRepository : IUserRepository
{
    public User Get(int id) => new(id, $"User {id}");
}

// This marker selects proxy services independently of ordinary scoped services.
public interface IProxyService;
public interface IGreetingService : IProxyService
{
    Guid ScopeId { get; }
    string SayHello(string message);
}

public sealed class GreetingService : IGreetingService
{
    public Guid ScopeId { get; } = Guid.NewGuid();
    public string SayHello(string message) => $"{message},hello world!";
}

// The application supplies this proxy; neither package needs to depend on the other.
public sealed class GreetingServiceProxy(IGreetingService target) : IGreetingService
{
    public Guid ScopeId => target.ScopeId;
    public string SayHello(string message)
        => target.SayHello(message + "【代理前置】") + "【代理后置】";
}
