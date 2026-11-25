# M.Simple.EntityFrameworkCore.SqlServer 接入与使用

适用 .NET 8 / EF Core 8，支持 SQL Server 2016+（兼容级别至少 130），自动依赖核心包。AppDbContext 替换为实际上下文并引入其命名空间；使用接收 DbContextOptions<AppDbContext> 的公共构造函数。

当前项目版本 `1.0.0.4`，目标框架 .NET 8。

## 安装包

在使用方项目目录执行，或通过 --project 指定项目：

```powershell
dotnet add package M.Simple.EntityFrameworkCore.SqlServer --version 1.0.0.4
```

包源需包含该版本。安装包不会自动执行注册；保留项目原有配置，无需添加示例接口。

## appsettings.json 配置

在 API 项目的 `appsettings.json` 中合并以下示例，替换为实际环境值：

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=your_database;Integrated Security=true;TrustServerCertificate=true;"
  }
}
```

对应 GetConnectionString("Default")。示例使用 Windows 集成认证；服务器及库名替换为实际值，其他认证或证书要求按环境配置。

真实凭据可通过 User Secrets 或环境变量覆盖，不必写入配置文件；环境变量将键中的冒号替换为双下划线，例如 `ConnectionStrings__Default`。

## Program.cs 配置

```csharp
using M.Simple.EntityFrameworkCore.SqlServer; // 本次新增：引入本包接入所需扩展或类型。

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// 本次新增：读取 SQL Server 连接串并注册上下文、仓储、工作单元及批量写入。
var sqlConnection = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
builder.Services.AddSimpleEfCoreSqlServer<AppDbContext>(sqlConnection);

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();
app.Run();
```

## 使用说明

在配置中提供 ConnectionStrings:Default，例如 Server=localhost;Database=your_database;Integrated Security=true;TrustServerCertificate=true。根据部署环境选择账号认证及证书配置。

通过构造函数注入 IBasicRepository<TEntity, TKey>、IBasicIntRepository<TEntity> 或 IBasicLongRepository<TEntity>，并注入 IUnitOfWork。普通修改调用 SaveChangesAsync；IBulkWriter 的直接写入立即执行。

[仓储、查询与工作单元事务](efcore.md)和[MySQL 适配](efcore-mysql.md)提供相关用法。



## 接入注意事项

原生批量写入使用 SqlBulkCopy，每批最多 1,000 条，参与 EF 当前事务或创建本次操作事务。BulkInsertAsync 不回填主键；BulkInsertAndReturnKeysAsync 支持数值 IDENTITY 单主键。批量更新、删除按主键执行并绕过 EF 查询过滤器及自动并发校验。注册不会自动建库、迁移或连接数据库。

## 业务仓储使用示例

先将实体加入实际 DbContext 模型，并维护数据库迁移。以下 Product 继承核心包的 Entity<long>，其 Id 对应 long 主键：

```csharp
using M.Simple.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public sealed class Product : Entity<long>
{
    public string Name { get; set; } = "";
}

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
}

public sealed class ProductService(
    IBasicLongRepository<Product> products, IUnitOfWork unitOfWork)
{
    public async Task<long> CreateAsync(string name, CancellationToken token)
    {
        var product = new Product { Name = name };
        products.Add(product);
        await unitOfWork.SaveChangesAsync(token);
        return product.Id;
    }

    public Task<Product?> GetAsync(long id, CancellationToken token)
        => products.GetByIdAsync(id, cancellationToken: token);

    public async Task<bool> RenameAsync(long id, string name, CancellationToken token)
    {
        var product = await products.GetByIdAsync(id, tracking: true, cancellationToken: token);
        if (product is null) return false;
        product.Name = name;
        await unitOfWork.SaveChangesAsync(token);
        return true;
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken token)
    {
        if (!await products.RemoveByIdAsync(id, token)) return false;
        await unitOfWork.SaveChangesAsync(token);
        return true;
    }
}
```

已有 AppDbContext 时合并实体模型，不另建第二个默认上下文。Build() 前注册 AddScoped<ProductService>()，由现有业务流程注入并调用。例如 productService 为已注入服务：

```csharp
long id = await productService.CreateAsync("商品 A", token);
var product = await productService.GetAsync(id, token);
bool renamed = await productService.RenameAsync(id, "商品 B", token);
bool deleted = await productService.DeleteAsync(id, token);
```

各调用按业务选择；DeleteAsync 为物理删除。查询默认不跟踪，修改时显式开启跟踪。事务、分页和批量写入的完整调用见[核心包示例](efcore.md)。
