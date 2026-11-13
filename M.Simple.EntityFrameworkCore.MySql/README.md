# M.Simple.EntityFrameworkCore.MySql

.NET 8 / EF Core 8 的 MySQL 适配包，自动依赖 `M.Simple.EntityFrameworkCore`，采用 Pomelo 8.0.3，提供基础仓储注册和 `MySqlBulkCopy` 批量新增。

```csharp
using M.Simple.EntityFrameworkCore;
using M.Simple.EntityFrameworkCore.MySql;
using Microsoft.EntityFrameworkCore;

services.AddSimpleEfCoreMySql<AppDbContext>(connectionString,
    new MySqlServerVersion(new Version(8, 0, 36)));
```

版本应填写实际 MySQL 版本。注册不会自动访问数据库。注入 `IRepository<TEntity, TKey>`、`IUnitOfWork`、`IBulkWriter` 使用。

批量新增需要连接串 `AllowLoadLocalInfile=true`、服务端开启 `local_infile`，并使用 InnoDB 事务表。每批 1,000 条，自动创建事务或参与 EF 当前事务，出现导入警告/行数不符会抛错。不回填生成主键，不运行保存拦截器；生成列由数据库负责。

首版仅支持简单单表映射。目标数据库为 MySQL 8.0；MariaDB 未作为首版验证目标。完整用法、事务规则、限制及测试步骤见核心包 README：`M.Simple.EntityFrameworkCore/README.md`。
