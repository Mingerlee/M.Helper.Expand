# M.Simple.EntityFrameworkCore 接入与使用

提供基础仓储、工作单元、事务与分页，不单独提供数据库驱动或原生批量写入。通常直接安装 MySQL 或 SQL Server 适配包，核心包会自动引入。此处的独立注册示例需宿主另行引用兼容的 SQL Server EF Core 8 提供程序，connectionString 为已有连接串，AppDbContext 为实际上下文。

当前项目版本 `1.0.0.4`，目标框架 .NET 8。

## 安装包

在使用方项目目录执行，或通过 --project 指定项目：

```powershell
dotnet add package M.Simple.EntityFrameworkCore --version 1.0.0.4
```

包源需包含该版本。安装包不会自动执行注册；保留项目原有配置，无需添加示例接口。

## Program.cs 配置

```csharp
using M.Simple.EntityFrameworkCore; // 本次新增：引入本包接入所需扩展或类型。
using Microsoft.EntityFrameworkCore; // 本次新增：引入本包接入所需扩展或类型。

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// 本次新增：使用已有数据库提供程序配置上下文，随后注册核心仓储和工作单元。
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddSimpleEfCore<AppDbContext>();

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();
app.Run();
```

## 使用说明

实体需实现 IEntity<TKey>，主键类型与仓储泛型参数一致；可继承 Entity<TKey>。只有一个名为 Id 的属性并不等于实现接口。核心注册不提供 IBulkWriter；使用适配包时不要重复注册默认上下文。

## 仓储接口（1.0.0.4）

通用仓储接口由 `IRepository<TEntity, TKey>` 改名为 `IBasicRepository<TEntity, TKey>`，保留主键类型参数和原有方法。升级时需要同步修改使用方的接口引用及自定义实现。

常用主键类型可直接使用简写接口：

```csharp
IBasicIntRepository<Category> categories; // Category 实现 IEntity<int>
IBasicLongRepository<Product> products;  // Product 实现 IEntity<long>
IBasicRepository<Label, Guid> labels;    // 其他主键类型继续使用通用接口
```

两个简写接口分别继承 `IBasicRepository<TEntity, int>` 和 `IBasicRepository<TEntity, long>`，拥有相同方法。`AddSimpleEfCore` 及 SQL Server / MySQL 适配包注册方法会自动注册三种接口，均为 Scoped，共用当前作用域的 DbContext 和工作单元；通用接口与简写接口不保证是同一个仓储对象。


## 基础 CRUD

通过构造函数注入 `IBasicRepository<Product, long>` 与 `IUnitOfWork`：

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
| RollbackTransactionAsync | 成功回滚工作单元事务后，自动清空 ChangeTracker，放弃所有尚未保存的修改 |
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
- 成功回滚后自动调用 `ChangeTracker.Clear()`，分离当前 DbContext 的全部实体，包括事务前已跟踪的实体和未保存的新增、修改、删除，无需业务再次调用。不会还原实体对象的属性或主键；继续操作应重新查询并跟踪实体。回滚失败时不清理跟踪，修复后可再次回滚；释放工作单元及外部直接回滚不触发此清理。此行为从 1.0.0.3 起提供。
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


## 仓储与批量写入补充（1.0.0.3）

以下七个方法从 `1.0.0.3` 起提供，核心、SQL Server、MySQL 包已同步实现。自定义 `IBasicRepository` / `IBulkWriter` 实现需要补齐新增接口成员。

| 方法 | 执行时机 | 返回 |
|---|---|---|
| `repository.Update(entity)` | 标记普通标量字段修改，需 SaveChanges | 无 |
| `repository.UpdateRange(entities)` | 同上，不是原生批量 SQL | 无 |
| `repository.ExecuteUpdateAsync(predicate, setters, token)` | 立即执行按条件更新 | 数据库影响行数 |
| `repository.ExecuteDeleteAsync(predicate, token)` | 立即执行按条件删除 | 数据库影响行数 |
| `bulk.BulkUpdateAsync(entities, options, token)` | 按主键原生批量更新 | 数据库影响行数 |
| `bulk.BulkDeleteAsync(entities, token)` | 按主键原生批量删除 | 数据库影响行数 |
| `bulk.BulkInsertAndReturnKeysAsync(entities, token)` | 立即插入并回填数值生成主键 | Task，主键回填到输入对象 |

```csharp
// 跟踪更新：复用上下文中相同主键的实体，只复制允许更新的普通标量属性。
products.Update(product);
products.UpdateRange(otherProducts);
await unitOfWork.SaveChangesAsync(token);

// 立即执行，不需要 SaveChanges；遵守 EF 全局查询过滤器。
int updated = await products.ExecuteUpdateAsync(p => p.Price < 0,
    setters => setters.SetProperty(p => p.Price, 0m), token);
int deleted = await products.ExecuteDeleteAsync(p => !p.Enabled, token);

// 强类型字段选择；省略 options 或 Properties 为空时更新全部允许更新的字段。
int bulkUpdated = await bulkWriter.BulkUpdateAsync(entities,
    new BulkUpdateOptions<Product> { Properties = [p => p.Price, p => p.Enabled] }, token);
int bulkDeleted = await bulkWriter.BulkDeleteAsync(entities, token);

// 输入应为主键默认值且未被 DbContext 跟踪的独立对象，不能先调用 Add/AddRange。
var inserted = new[] { new Product { Name = "new", Price = 10 } };
await bulkWriter.BulkInsertAndReturnKeysAsync(inserted, token);
Console.WriteLine(inserted[0].Id);
```

行为与限制：

- `Update` / `UpdateRange` 不递归更新导航属性；排除主键、数据库生成列及并发令牌，不提供字段选择的属性不会因为本方法而被标记修改。完整更新会覆盖传入对象的默认值；避免用缺少字段的 DTO 直接构造完整更新对象。并发令牌沿用 EF 原有校验，分离实体应携带正确原始令牌；复杂状态请使用跟踪查询后修改。
- 条件更新、删除由 EF 翻译为集合 SQL，遵守查询过滤器；不执行自动客户端并发校验，需要时将版本条件加入 predicate 并检查影响行数。条件不可为 null，要操作全部数据须显式传入 `p => true`。
- 原生 bulk 更新、删除按主键定位，**绕过全局查询过滤器、软删除及 EF 并发校验**；仅对调用方已授权的键执行。支持单一 CLR 主键，其他单表、列映射、转换器限制与原生新增相同；不支持复合键、继承或共享表。
- 原生更新排除主键、计算/生成列和并发令牌，指定非法字段立即抛错；按主键删除为物理删除。不存在的键不报错，也不会插入新记录。重复键不支持；CLR 键跨批次去重校验，单批次额外按数据库排序规则校验。
- SQL Server / MySQL 原生更新、删除先批量写临时表，再 JOIN 更新或删除。每批最多 1,000 条；不会为了 staging 结束外层事务。MySQL 需要临时表权限及原生导入配置；目标表必须使用 InnoDB 等支持事务的引擎。影响行数沿用提供程序语义，MySQL 的 `UseAffectedRows` 设置会影响“匹配行数/实际变更行数”。
- 新增主键回填仅支持 `short` / `int` / `long` 的单一数据库生成主键，且至少有一个普通可写列；MySQL 要求 AUTO_INCREMENT，SQL Server 要求 IDENTITY。SQL Server 使用 staging + MERGE OUTPUT 关联输入序号，不依赖输出顺序；MySQL 在同一事务中逐条 INSERT 并读取 LAST_INSERT_ID，不按连续自增值推算，速度低于不回填的 BulkInsertAsync。显式 Guid/string 主键继续使用 BulkInsertAsync。
- 回填模式只在全部批次成功后修改对象主键；某批失败则不回填，自己的事务会回滚。已有外层事务时只参与、不提交：后续外层回滚不会恢复已回填的对象主键，应重新构造对象。数据库可能因回滚留下自增空洞。回填暂存实体和键、更新删除的重复键校验会占用随输入数量增长的内存；无回填新增仍保持按批控制。
- 所有直接操作立即执行，不依赖 SaveChanges、不更新或清空 ChangeTracker，也不自动保存上下文中其他修改。混用跟踪实体时应重新加载或明确分离，避免旧状态覆盖数据库；同一个 DbContext 不能并行执行。
- 异常和取消向上抛出；失败时不返回空集合或 false。无已有事务时整次 bulk 调用统一提交或回滚；有事务时由调用方负责回滚，尤其不能在部分批次成功后继续提交。提交或清理阶段异常可能发生在数据库已提交之后，不应盲目重试。



## 接入注意事项

实体需实现 IEntity<TKey>，主键类型与仓储泛型参数一致；可继承 Entity<TKey>。只有一个名为 Id 的属性并不等于实现接口。核心注册不提供 IBulkWriter；使用适配包时不要重复注册默认上下文。
