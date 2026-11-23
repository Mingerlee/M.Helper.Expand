# M.Simple.Container Web 示例

```powershell
dotnet run --project M.Simple.Container.Sample -c Release -- --urls http://127.0.0.1:5198
```

- `GET /users/1`：演示业务层依赖仓储接口。
- `GET /scope`：`sharedInstance` 为 `true`，重复请求的 `scopeId` 不同。

入口只需 `UseSimpleContainer()` 和 `AddAssemblyServices<IScopedService>(...)`；接口继承标记即可参与批量注入。
