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
