# M.Simple.AspNetCore.Response.Swagger

M.Simple.AspNetCore.Response 的可选 Swagger 适配包，面向 .NET 8 与 Swashbuckle.AspNetCore **6.x**（6.4.0 起，不兼容 OpenAPI 模型不同的新版主版本）。自动依赖核心响应包，不依赖完整 Swagger UI 包。

```powershell
dotnet add package M.Simple.AspNetCore.Response.Swagger --version 1.0.0.1
```

在已有 Swagger 注册中增加一行：

```csharp
using M.Simple.AspNetCore.Response;
using M.Simple.AspNetCore.Response.Swagger;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddUnifiedResponse();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // 由宿主引用的 Swashbuckle.AspNetCore 提供。
builder.Services.AddUnifiedResponseSwagger();

var app = builder.Build();
app.UseUnifiedResponse();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapControllers();
app.Run();
```

- 将普通 JSON 响应文档包装为 code、message、data，保留原始 DTO/数组的 data Schema。
- 补充默认 400 校验错误和 500 异常响应文档；204 文档改为带响应体的 200。
- 已声明 ApiResponse<T> 的模型不重复包装；`[SkipUnifiedResponse]` 操作保持原文档。
- 显式文件、文本及非 JSON 响应不包装。返回 IActionResult 的动态文件/文本接口，建议用 `[Produces("application/octet-stream")]` / `[Produces("text/plain")]` 标记，或使用 `[SkipUnifiedResponse]`，以便文档准确反映运行时行为。
- 只改动 Swagger 文档，运行时统一响应仍需 AddUnifiedResponse / UseUnifiedResponse。

如果已声明自定义错误响应，以该操作的声明为准。Swagger 的默认描述为中文，运行时自定义消息不改变描述。
