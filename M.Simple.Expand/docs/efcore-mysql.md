# M.Simple.EntityFrameworkCore.MySql 接入与使用

面向 .NET 8 / EF Core 8 的 MySQL 适配包，当前版本 `1.0.0.4`，使用 Pomelo.EntityFrameworkCore.MySql `8.0.3`。提供 DbContext、基础仓储、工作单元和原生批量写入注册，自动依赖核心包 `M.Simple.EntityFrameworkCore`。

## 安装包

在 API 项目目录执行，或通过 `--project` 指定项目：

```powershell
dotnet add package M.Simple.EntityFrameworkCore.MySql --version 1.0.0.4
```

如果实体、DbContext 或仓储接口位于独立类库，这些类库也需要相应的核心包或 MySQL 适配包引用。不要混用不兼容的 EF Core 主版本。

## Program.cs 配置

在现有 API 中加入数据库配置，保留已有的容器、Swagger、统一响应及认证配置，无需添加示例接口。`AppDbContext` 替换为项目实际上下文类型，并引入其所在命名空间。

```csharp
using M.Simple.EntityFrameworkCore.MySql; // 本次新增：MySQL 数据访问注册扩展。
using Microsoft.EntityFrameworkCore; // 本次新增：MySqlServerVersion 类型。

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 本次新增：读取连接串，未配置时明确报错。
var mysqlConnection = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

// 本次新增：注册上下文、基础仓储、工作单元和批量写入；填写实际 MySQL 版本。
builder.Services.AddSimpleEfCoreMySql<AppDbContext>(mysqlConnection,
    new MySqlServerVersion(new Version(8, 0, 46)));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

显式指定服务器版本时，注册不会自动访问数据库，也不会自动创建数据库、建表或执行迁移。不需要额外调用 `AddSimpleEfCore()` 或为同一上下文重复调用 `AddDbContext()`。

## 配置连接串

使用 `ConnectionStrings:Default`，例如以下配置结构。示例中的账号、密码、库名需要替换；本地开发可放在 User Secrets 中。

```json
{
  "ConnectionStrings": {
    "Default": "Server=127.0.0.1;Port=3306;Database=your_database;User ID=your_user;Password=your_password;"
  }
}
```

普通 CRUD 不要求启用原生导入。使用原生批量导入时，连接串还需加入 `AllowLoadLocalInfile=true`，服务端开启 `local_infile`，目标表使用 InnoDB 等支持事务的存储引擎。

## DbContext 与实体

已有上下文时沿用现有映射；上下文应提供接收 `DbContextOptions<TContext>` 的公共构造函数。下面展示最小结构，不是接口示例：

```csharp
using M.Simple.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

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

使用基础仓储时，实体必须实现与主键类型一致的 `IEntity<TKey>`；继承 `Entity<TKey>` 是可选方式。只存在一个名为 Id 的属性并不等于已实现接口。已有实体可直接实现接口，不必更换原有基类。

## 仓储与工作单元

以下接口均位于 `M.Simple.EntityFrameworkCore`，注册方法会自动将其按 Scoped 生命周期注册：

| 接口 | 用途 |
| --- | --- |
| IBasicRepository<TEntity, TKey> | 任意受支持主键类型的基础仓储 |
| IBasicIntRepository<TEntity> | int 主键仓储，实体实现 IEntity<int> |
| IBasicLongRepository<TEntity> | long 主键仓储，实体实现 IEntity<long> |
| IUnitOfWork | 保存当前作用域修改及管理事务 |
| IBulkWriter | 立即执行原生批量写入 |

`1.0.0.4` 已将原 `IRepository<TEntity, TKey>` 改名为 `IBasicRepository<TEntity, TKey>`，升级时同步修改引用。泛型第一个参数是实体类型，第二个参数是主键类型，例如 `IBasicRepository<Product, long>`。

在业务类或仓储类中通过构造函数注入，沿用项目现有的服务注册或程序集扫描：

```csharp
using M.Simple.EntityFrameworkCore;

public sealed class ProductService(
    IBasicLongRepository<Product> products,
    IUnitOfWork unitOfWork)
{
    public Task<Product?> GetAsync(long id, CancellationToken token)
        => products.GetByIdAsync(id, cancellationToken: token);

    public async Task<long> AddAsync(Product product, CancellationToken token)
    {
        products.Add(product);
        await unitOfWork.SaveChangesAsync(token);
        return product.Id;
    }
}
```

查询默认不跟踪。需要修改查询结果时，使用 `GetByIdAsync(id, tracking: true, cancellationToken: token)`，修改后调用 `SaveChangesAsync()`。`Add`、`Update`、`Remove` 及其 Range 方法需要保存；删除是物理删除。

仓储、工作单元和批量写入共享同一作用域的 DbContext，每个容器只支持一个默认业务上下文。同一个 DbContext 不可跨线程并行使用。

## 查询与分页

以下片段中的 `products` 为已注入的仓储，`token` 为当前取消令牌：

```csharp
using M.Simple.EntityFrameworkCore;

var page = await products.Query()
    .OrderBy(p => p.Id)
    .Select(p => new { p.Id, p.Name })
    .ToPageAsync(pageNumber: 1, pageSize: 20, cancellationToken: token);
```

返回 Items、TotalCount、PageNumber、PageSize、TotalPages。页码从 1 开始，默认每页 20 条、最大 200 条。必须显式排序，并包含唯一键以保证同值排序的次序。

## 工作单元事务

```csharp
await unitOfWork.BeginTransactionAsync(token);
try
{
    products.Add(product);
    await unitOfWork.SaveChangesAndCommitAsync(token);
}
catch (Exception original)
{
    try { await unitOfWork.RollbackTransactionAsync(CancellationToken.None); }
    catch (Exception rollback) { original.Data["RollbackException"] = rollback; }
    throw;
}
```

- `SaveChangesAsync()` 保存修改，不提交已经开启的事务。
- `CommitTransactionAsync()` 只提交事务，不自动保存尚未保存的修改。
- `SaveChangesAndCommitAsync()` 要求先 Begin，再保存并提交；失败不自动回滚。
- `RollbackTransactionAsync()` 成功后清空当前上下文的 ChangeTracker，不还原实体对象的属性或主键。
- 工作单元只管理由自身开启的事务，不支持嵌套事务，也不覆盖 HTTP、邮件等外部操作。

## 批量写入

通过构造函数注入 `IBulkWriter`。以下 `entities` 为当前操作实体集合：

```csharp
await bulkWriter.BulkInsertAsync(entities, token);
await bulkWriter.BulkUpdateAsync(entities,
    new BulkUpdateOptions<Product> { Properties = [p => p.Price] }, token);
await bulkWriter.BulkDeleteAsync(entities, token);
```

上述代码分别展示新增、更新、删除调用，按实际业务选择，不表示对同一批数据连续执行。操作立即执行，无需 `SaveChangesAsync()`；默认每批最多 1,000 条，有 EF 当前事务时参与该事务，否则为本次操作创建事务。

- 批量新增默认不回填生成主键，也不运行 SaveChanges 拦截器。需回填时使用 `BulkInsertAndReturnKeysAsync()`，输入应为未跟踪且主键为默认值的独立实体，不能先 Add / AddRange。
- MySQL 主键回填仅支持 short / int / long 的 AUTO_INCREMENT 单主键，使用同一事务逐条插入并读取生成键，速度低于不回填的批量导入。
- 原生批量更新、删除按主键定位，绕过 EF 全局查询过滤器、软删除及自动并发校验；只传入业务已确认允许操作的实体。删除为物理删除。
- 支持简单单表映射；不支持复合键、继承映射、拆表、共享表或 Owned 类型，不递归保存关联实体。
- 批量写入不刷新已跟踪实体。有外层事务且操作失败时，由调用方回滚；回滚不会恢复已回填到对象的主键。

## 常见问题

1. 找不到 ConnectionStrings:Default：检查运行环境配置、User Secrets 或环境变量是否提供该键。
2. 实体或泛型约束报错：确认实体实现 IEntity<TKey>，Id、接口及仓储的主键类型一致。
3. 提示无法解析 DbContext 或仓储：确认在 Build() 前调用 AddSimpleEfCoreMySql<TContext>()，且业务服务已注册。
4. 查询成功但修改未落库：检查是否调用 SaveChangesAsync，以及查询结果是否开启跟踪或显式调用 Update。
5. 批量导入失败：检查 AllowLoadLocalInfile、服务端 local_infile、表引擎和字段映射；批量更新、删除还需临时表权限。

目标数据库为 MySQL 8.0；MariaDB 未作为此版本的验证目标。本页为接入说明，更新文档不会连接数据库或执行迁移、写入操作。
