using M.Simple.Container;
using M.Simple.Container.Sample;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSimpleContainer();
builder.Services.AddAssemblyServices<IScopedService>(typeof(UserService).Assembly);

var app = builder.Build();
app.MapGet("/", () => "M.Simple.Container: try /users/1 and /scope");
app.MapGet("/users/{id:int}", (int id, IUserService users) => users.Get(id));
app.MapGet("/scope", (IUserService users, IUserAudit audit) => new
{
    users.ScopeId,
    SharedInstance = ReferenceEquals(users, audit)
});
app.Run();
