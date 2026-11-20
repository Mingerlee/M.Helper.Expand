# M.Simple.EntityFrameworkCore

.NET 8 / EF Core 8 基础数据访问包，当前版本为 `1.0.0.2`。提供单主键实体仓储、工作单元、条件查询、DTO 分页和原生批量写入契约。

## 安装与注册

通常只安装需要的适配包，核心包自动作为依赖引入：

```powershell
dotnet add package M.Simple.EntityFrameworkCore.SqlServer --version 1.0.0.2
# 或
dotnet add package M.Simple.EntityFrameworkCore.MySql --version 1.0.0.2
```

上述安装命令用于包发布后或配置本地包源后。本仓库默认只本地打包，不自动发布 NuGet。

```csharp
using M.Simple.EntityFrameworkCore;
using M.Simple.EntityFrameworkCore.SqlServer;
using Microsoft.EntityFrameworkCore;

services.AddSimpleEfCoreSqlServer<AppDbContext>(connectionString);

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().Property(p => p.Name).HasMaxLength(100);
        modelBuilder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
    }
}

public sealed class Product : Entity<long>
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}
```

MySQL 改为以下注册；显式传入实际服务器版本，注册时不会访问数据库：

```csharp
using M.Simple.EntityFrameworkCore.MySql;

services.AddSimpleEfCoreMySql<AppDbContext>(connectionString,
    new MySqlServerVersion(new Version(8, 0, 36)));
```

需要自行配置 DbContext 时：

```csharp
services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
services.AddSimpleEfCore<AppDbContext>(); // 只注册核心仓储与工作单元，不注册原生批量写入
```

每个容器只支持一个默认业务上下文，重复注册会抛错。仓储、工作单元、批量写入共享同一作用域的上下文，不能跨线程并行使用。派生上下文应提供接收 `DbContextOptions<TContext>` 的公共构造函数。

## 基础 CRUD

通过构造函数注入 `IRepository<Product, long>` 与 `IUnitOfWork`：

```csharp
var product = new Product { Name = "示例商品", Price = 10 };
products.Add(product);
await unitOfWork.SaveChangesAsync(token); // 此时写入数据库并回填普通新增的主键

var item = await products.GetByIdAsync(product.Id); // 默认不跟踪，未找到返回 null
var exists = await products.AnyAsync(p => p.Name == "示例商品", token);
var count = await products.CountAsync(cancellationToken: token);
var list = await products.ListAsync(p => p.Price > 0, token);

var tracked = await products.GetByIdAsync(product.Id, tracking: true,
    cancellationToken: token);
if (tracked is not null)
{
    tracked.Price = 20;
    await unitOfWork.SaveChangesAsync(token);
}

// 无需查询时，可指定更新字段；主键、生成字段和并发令牌禁止在此更新。
products.UpdateProperties(new Product { Id = product.Id, Price = 30 }, p => p.Price);
await unitOfWork.SaveChangesAsync(token);

var found = await products.RemoveByIdAsync(product.Id, token); // 查不到返回 false
if (found) await unitOfWork.SaveChangesAsync(token);
```

`AddRange` / `RemoveRange` 用于多个实体，仍通过 EF 跟踪和保存，并非原生批量 SQL。`Remove`、`RemoveRange` 和 `RemoveByIdAsync` 均为物理删除。多仓储修改通过同一个 `IUnitOfWork` 一次保存。

实体实现 `IEntity<TKey>` 即可，继承 `Entity<TKey>` 为可选；首版约定 `Id` 为唯一主键，不支持复合主键仓储。字符串、Guid、整数主键的生成规则由 EF 模型决定。

`UpdateProperties` 会复用同主键的已跟踪实体，并仅复制指定字段。上下文中其他已有修改仍会随保存提交，不会被清除。对带并发令牌的实体，优先查询跟踪实体后修改，以保留正确的原始令牌；扩展包不隐藏并发冲突或数据库异常。

## 工作单元事务

新增 BeginTransactionAsync、CommitTransactionAsync、RollbackTransactionAsync、SaveChangesAndCommitAsync，适用于 SQL Server / MySQL 关系型上下文。从 1.0.0.2 起提供这些 API。

```csharp
await unitOfWork.BeginTransactionAsync(token);
try
{
    // 通过 AOP 接口代理调用时，会等待前置、业务、后置全部完成。
    await service.SayHolle(msg, token);

    int affected = await unitOfWork.SaveChangesAndCommitAsync(token);
}
catch (Exception original)
{
    try { await unitOfWork.RollbackTransactionAsync(CancellationToken.None); }
    catch (Exception rollback) { original.Data["RollbackException"] = rollback; }
    throw;
}
```

| 方法 | 行为 |
|---|---|
| BeginTransactionAsync | 开启工作单元拥有的事务，不支持重复开启或嵌套 |
| SaveChangesAsync | 保存当前修改；不提交已有事务，无显式事务时保留 EF 原有默认行为 |
| CommitTransactionAsync | 提交工作单元事务，不自动保存尚未保存的修改 |
| RollbackTransactionAsync | 回滚工作单元事务，不自动清空或恢复 ChangeTracker |
| SaveChangesAndCommitAsync | 先校验已开启工作单元事务，再保存并提交；返回本次 SaveChanges 的影响条目数，不包含此前保存的条目 |

没有 Begin 时，后三个事务结束方法均抛 InvalidOperationException，提示“请先调用 BeginTransactionAsync 开启事务。”。SaveChangesAndCommitAsync 会在任何写入前进行此检查。

需要中途取得自增 ID 或查询中间结果时，可以在同一事务内多次 SaveChangesAsync，再通过 CommitTransactionAsync 一起提交；任一阶段失败可全部回滚：

```csharp
await unitOfWork.BeginTransactionAsync(token);
try
{
    products.Add(new Product { Name = "阶段一", Price = 10 });
    await unitOfWork.SaveChangesAsync(token);
    products.Add(new Product { Name = "阶段二", Price = 20 });
    await unitOfWork.SaveChangesAsync(token);
    await unitOfWork.CommitTransactionAsync(token);
}
catch (Exception original)
{
    try { await unitOfWork.RollbackTransactionAsync(CancellationToken.None); }
    catch (Exception rollback) { original.Data["RollbackException"] = rollback; }
    throw;
}
```

事务边界与限制：

- 工作单元、仓储和前后置方法必须使用同一 Scoped DbContext。事务包含数据库操作，不自动覆盖邮件、HTTP、Redis 等外部副作用。
- 事务保存或提交失败（包括取消）后，后续保存/提交会被拒绝，必须显式回滚或结束作用域；SaveChangesAndCommitAsync 不自动回滚。业务自身抛错但未调用保存时，仍需调用方按约定回滚，不能吞掉异常后提交。
- 成功提交或回滚后释放事务，可再开启新事务；事务结束方法没有活动事务时不会静默成功。Commit 发生网络错误时实际提交结果可能不确定，不应自动重试。
- 只管理由本工作单元 Begin 创建的事务。遇到 DbContext 外部事务或环境事务时拒绝 Begin，不接管、不提交或回滚外部事务；普通 SaveChangesAsync 仍可参与外部事务，由其所有者管理。
- 工作单元实现 IDisposable/IAsyncDisposable，由 DI 在作用域结束时释放未结束事务（关系型提供程序通常回滚未提交事务），从不自动提交，也不释放由 DI 单独管理的 DbContext。
- 回滚不会还原跟踪实体的状态、主键或内存对象值；失败后建议结束作用域，重试时使用新 DbContext。若确需复用，由业务明确处理 ChangeTracker。
- 不支持跨线程并行操作；不要在工作单元之外直接提交、替换或释放它拥有的事务。对这类事务归属变化会在保存/提交前检查并报错。
- 不自动启用重试。配置提供程序重试策略时，事务与执行策略的组合仍由调用方负责；不要无条件重放含外部副作用的业务。
- 提交成功后的事务释放也可能抛异常，此时不能视为业务尚未提交；不能据此盲目重试业务。
- IUnitOfWork 新增接口成员，自定义实现者升级源码时也需实现这些成员。

## 查询与分页

```csharp
var page = await products.Query()
    .WhereIf(!string.IsNullOrWhiteSpace(keyword), p => p.Name.Contains(keyword!))
    .OrderBy(p => p.Id)
    .Select(p => new { p.Id, p.Name })
    .ToPageAsync(pageNumber: 1, pageSize: 20, cancellationToken: token);
```

返回 `Items`、`TotalCount`、`PageNumber`、`PageSize`、`TotalPages`。页码从 1 开始，默认每页 20 条，最大 200 条，非法参数抛错。可给 `ToPageAsync` 传入 `options: new SimpleEfCoreOptions { MaxPageSize = 500 }`；这是单次调用选项，不是隐式的 DI 全局设置。

调用方必须显式排序，并包含唯一键作为同值排序的次序。总数和数据顺序执行两次查询，遇到并发写入时不保证快照一致，需要时自行配置事务隔离级别。关联、分组等查询继续使用原生 LINQ；`Query(true)` 开启跟踪。

## 大量数据操作

```csharp
// 需安装并注册对应适配包，通过 DI 注入 IBulkWriter。
await bulkWriter.BulkInsertAsync(entities, token);

// EF 原生条件更新、删除：立即执行，无需 SaveChangesAsync。
await products.Query().Where(p => p.Price < 0)
    .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Price, 0m), token);
await products.Query().Where(p => p.Price == 0).ExecuteDeleteAsync(token);
```

批量新增使用 SQL Server `SqlBulkCopy` 或 MySQL `MySqlBulkCopy`，每批 1,000 条，内存按批控制，不会为每条数据生成跟踪条目。

首版批量写入约定：

- 立即执行，绕过 `SaveChanges` 拦截器，不执行 EF 客户端主键生成，不回填主键，不改变实体跟踪状态。
- 支持简单单表实体、列名映射、标量值转换；不支持继承映射、拆表、共享表、Owned 类型、需要写入的 shadow/field-only 属性。不递归保存关联实体。
- 仅写入配置为 `ValueGenerated.Never` 的属性；生成列由数据库负责，传入值会被忽略。Guid / 字符串主键必须显式 `ValueGeneratedNever()` 并自行赋值。数字生成主键应配置为数据库自增。
- 无现有事务时，创建事务统一提交所有批次，失败或取消时回滚；存在 EF 当前事务时参与该事务，由调用方提交或回滚。失败后调用方必须回滚其事务，不能继续提交部分结果。
- 不支持隐式环境事务；使用 `Database.BeginTransactionAsync()`。如果启用连接重试，事务与重试策略的组合由调用方负责，批量写入不会自动重放。
- MySQL 需要连接串 `AllowLoadLocalInfile=true`、服务端开启 `local_infile`，使用 InnoDB 等事务表。批量导入出现警告或行数不匹配会抛错，避免静默丢行或截断。
- SQL Server 显式启用约束检查、触发器和保留 NULL。数据库默认值、触发器、索引及驱动行为仍存在数据库差异。

普通保存和直接批量操作属于不同执行路径。混用时需自行管理事务和已跟踪实体的刷新，`SaveChangesAsync` 不会自动包含此前已经提交的直接操作。

## 版本与验证

- 目标框架：.NET 8；EF Core / SQL Server provider：8.0.22；Pomelo MySQL provider：8.0.3。
- 首版目标数据库：SQL Server 2016+（兼容级别至少 130）、MySQL 8.0。迁移按数据库分别维护，字符串排序、大小写和数据库函数沿用各自数据库语义。
- 关系型 CRUD、分页和共享事务管线通过 SQLite 测试；两种提供程序另有注册与 SQL 翻译测试。真实服务器集成测试为显式启用，不把 SQLite 或 SQL 翻译通过等同于双库实测。

```powershell
dotnet test M.Simple.EntityFrameworkCore.Test/M.Simple.EntityFrameworkCore.Test.csproj

# 可选真实数据库测试：账号需能创建/删除独立测试库，不要使用业务环境。
$env:MSIMPLE_EF_SQLSERVER = 'Server=localhost;Integrated Security=true;TrustServerCertificate=true'
$env:MSIMPLE_EF_MYSQL = 'Server=localhost;User ID=test;Password=...;AllowLoadLocalInfile=true'
dotnet test M.Simple.EntityFrameworkCore.Test/M.Simple.EntityFrameworkCore.Test.csproj --logger 'console;verbosity=normal'
```

真实服务器测试会创建唯一的 `msimple_ef_test_<guid>` 数据库，测试结束删除，仅在该库内执行 CRUD、10,000 条普通/原生批量写入、条件更新删除和跨批次回滚验证。控制台输出耗时，不承诺任意环境下秒级。

2026-11-13 本机 Release 验证：SQL Server 2016（13.0.5026.0）真实集成测试通过；简单表 10,000 条普通新增约 1.428 秒，原生批量新增约 0.737 秒，仅作为一次本机实测参考。MySQL 已通过编译、DI 注册及 SQL 翻译测试，尚未进行真实服务器写入验证。测试结果保存在 `artifacts/efcore-tests/efcore.trx`。

本地包输出：`artifacts/efcore-packages`。在使用方项目的 `NuGet.Config` 中加入本地源，并保留公共依赖源：

```xml
<configuration>
  <packageSources>
    <add key="MSimpleLocal" value="D:\oym\M.Helper.Expand\artifacts\efcore-packages" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

然后给项目添加适配包：

```powershell
dotnet add <项目路径> package M.Simple.EntityFrameworkCore.SqlServer --version 1.0.0.2
```

依赖还原还需启用 nuget.org 或具有对应 EF Core / 数据库驱动依赖的包源。源码示例使用项目引用，不依赖公开发布。

可运行示例见 `M.Simple.EntityFrameworkCore.Sample`，初版未包含软删除、审计、多租户和缓存。
