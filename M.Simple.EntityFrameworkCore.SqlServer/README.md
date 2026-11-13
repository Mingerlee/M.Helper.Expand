# M.Simple.EntityFrameworkCore.SqlServer

.NET 8 / EF Core 8 的 SQL Server 适配包，自动依赖 `M.Simple.EntityFrameworkCore`，提供基础仓储注册和 `SqlBulkCopy` 批量新增。

```csharp
using M.Simple.EntityFrameworkCore;
using M.Simple.EntityFrameworkCore.SqlServer;

services.AddSimpleEfCoreSqlServer<AppDbContext>(connectionString);
```

注入 `IRepository<TEntity, TKey>`、`IUnitOfWork`、`IBulkWriter` 使用。普通新增、修改、删除需调用工作单元保存；批量写入立即执行。

首版支持简单单表批量写入，每批 1,000 条，自动创建事务或参与 EF 当前事务，不回填生成主键，生成列由数据库负责。启用约束检查、触发器并保留 NULL。继承、Owned、共享表等复杂映射不支持；批量操作不经过保存拦截器。

支持目标：SQL Server 2016+（兼容级别至少 130）。完整用法、事务规则、限制及测试步骤见核心包 README：`M.Simple.EntityFrameworkCore/README.md`。
