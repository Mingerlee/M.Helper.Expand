using M.Simple.Services.Interception;

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
public interface IInterception;
public interface IGreetingService : IInterception
{
    Guid ScopeId { get; }
    string SayHello(string message);
}

public sealed class GreetingService : IGreetingService
{
    public Guid ScopeId { get; } = Guid.NewGuid();
    [BeforeMethod(nameof(Before)), AfterMethod(nameof(After))]
    public string SayHello(string message) => $"{message},hello world!";
    private void Before(MethodInvocationContext context)
        => context.SetArgument("message", context.GetArgument<string>("message") + "【代理前置】");
    private void After(MethodInvocationContext context)
        => context.SetResult(context.Result + "【代理后置】");
}
