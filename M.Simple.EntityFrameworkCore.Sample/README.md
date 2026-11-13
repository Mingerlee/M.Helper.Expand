# EF Core 首版示例

示例演示注册、普通新增、查询、指定字段更新、分页、删除和原生批量新增。双适配包引用仅为演示切换，业务项目只需选择一个。

```powershell
$env:MSIMPLE_EF_SAMPLE_CONNECTION = 'Server=localhost;Database=msimple_ef_sample_demo;Integrated Security=true;TrustServerCertificate=true'
dotnet run --project M.Simple.EntityFrameworkCore.Sample -- sqlserver
```

MySQL：填写专用数据库连接，并将参数改为 `mysql`；批量导入需要连接串 `AllowLoadLocalInfile=true` 和服务端 `local_infile=ON`。

示例仅接受名称以 `msimple_ef_sample_` 开头的数据库，必要时创建数据库/表；数据操作放在事务中，结束时回滚。专用数据库及表会保留，不会自动删除。
