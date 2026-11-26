using M.Simple.Container;
using M.Simple.Container.Sample;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSimpleContainer();
builder.Services.AddAssemblyServices<IScopedService>(typeof(UserService).Assembly);
builder.Services.Scan(typeof(GreetingService).Assembly)
    .AssignableTo<IInterception>()
    .ExcludeInterface(typeof(IInterception))
    .AsImplementedInterfaces()
    .InterceptedScoped();

var app = builder.Build();
app.MapGet("/", () => "M.Simple.Container: try /users/1, /scope and /proxy?message=hello");
app.MapGet("/proxy", (string message, IGreetingService greeting) => new
{
    Result = greeting.SayHello(message),
    greeting.ScopeId,
    ProxyType = greeting.GetType().Name
});
app.MapGet("/users/{id:int}", (int id, IUserService users) => users.Get(id));
app.MapGet("/scope", (IUserService users, IUserAudit audit) => new
{
    users.ScopeId,
    SharedInstance = ReferenceEquals(users, audit)
});
app.Run();
