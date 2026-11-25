# M.Simple.EntityFrameworkCore.SqlServer 接入与使用

适用 .NET 8 / EF Core 8，支持 SQL Server 2016+（兼容级别至少 130），自动依赖核心包。AppDbContext 替换为实际上下文并引入其命名空间；使用接收 DbContextOptions<AppDbContext> 的公共构造函数。

当前项目版本 `1.0.0.4`，目标框架 .NET 8。

## 安装包

在使用方项目目录执行，或通过 --project 指定项目：

```powershell
dotnet add package M.Simple.EntityFrameworkCore.SqlServer --version 1.0.0.4
```

包源需包含该版本。安装包不会自动执行注册；保留项目原有配置，无需添加示例接口。

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
