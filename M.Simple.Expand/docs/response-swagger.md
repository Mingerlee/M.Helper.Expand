# 统一响应与 Swagger 接入

`M.Simple.AspNetCore.Response.Swagger` 为 ASP.NET Core MVC 控制器提供统一响应的 Swagger 文档适配。适用 .NET 8、Swashbuckle.AspNetCore 6.x（6.4.0 起，低于 7.0.0）。

## 安装包

在 Web API 项目目录执行，或通过 `--project` 指定项目：

```powershell
dotnet add package M.Simple.AspNetCore.Response.Swagger --version 1.0.0.1
dotnet add package Swashbuckle.AspNetCore --version 6.4.0
```

Swagger 适配包会自动依赖 `M.Simple.AspNetCore.Response` 核心包，无需再次单独安装核心包。已有兼容版本 Swashbuckle.AspNetCore 的项目保留现有引用即可。

## Program.cs 配置

在现有项目中补齐以下注册和中间件。已有 Swagger 分组、数据库、依赖注入和认证配置继续保留，无需添加示例接口。

```csharp
using M.Simple.AspNetCore.Response; // 本次新增：统一响应运行时扩展。
using M.Simple.AspNetCore.Response.Swagger; // 本次新增：Swagger 文档适配扩展。

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddUnifiedResponse(); // 本次新增：注册统一响应包装、参数校验响应及异常处理服务。
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddUnifiedResponseSwagger(); // 本次新增：将 Swagger 响应文档适配为 code、message、data。

var app = builder.Build();

app.UseUnifiedResponse(); // 本次新增：启用异常及空错误响应统一处理，放在认证、授权和接口执行之前。

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

`AddUnifiedResponse()` 内部已调用 `AddControllers()`，也兼容项目已有的 MVC 注册。

## 三个调用分别负责什么

| 调用 | 作用 | 放置位置 |
| --- | --- | --- |
| AddUnifiedResponse() | 注册 MVC 结果包装、自动参数校验响应及异常处理服务 | builder.Build() 之前 |
| UseUnifiedResponse() | 启用异常处理和空错误响应的统一输出 | builder.Build() 之后，认证、授权及接口执行之前 |
| AddUnifiedResponseSwagger() | 将 Swagger JSON 响应模型描述为 code、message、data | AddSwaggerGen() 之后，builder.Build() 之前 |

**只引入包或只调用 AddUnifiedResponseSwagger()，不会改变接口的真实返回值。** 要让实际响应与 Swagger 文档一致，必须同时配置运行时的 `AddUnifiedResponse()` 和 `UseUnifiedResponse()`。

## 现有接口的返回效果

控制器继续直接返回原有 DTO、实体或 `Ok(value)`，无需继承专用基类，也无需在每个接口中手动包装。原业务数据会放入 `data`：

```json
{
  "code": 200,
  "message": "成功",
  "data": {
    "id": 4,
    "applyCode": "apply_code_value"
  }
}
```

- `code` 默认与实际 HTTP 状态一致。参数校验失败返回 400；未处理异常返回 500、“服务器内部错误”和 null 数据。
- 普通 DTO、数组及 JSON 结果自动包装；已返回 `ApiResponse<T>` 的结果不重复包装。
- 文件、重定向、显式 ContentResult、流式响应保留协议语义；本包不会自动包装 Minimal API。
- 使用 `[SkipUnifiedResponse]` 可跳过指定控制器或操作的统一响应及对应 Swagger 文档改写。

## 排查：文档已包装，实际响应仍直接返回实体

Swagger 的 `Server response / Response body` 显示真实请求结果，下面的 Schema 或 Example Value 来自文档定义。两者不同通常是只启用了文档适配。

1. 确认 Program.cs 引入两个命名空间，并调用上述三个方法。
2. 重新编译、重启实际监听端口的 API 进程，再执行 Swagger 请求。
3. 如仍未包装，检查请求是否命中当前项目、控制器或操作是否有 `[SkipUnifiedResponse]`，以及是否返回文件、文本或其他不包装的结果。

自定义 Swagger 分组继续使用项目原来的 `SwaggerDoc` 和 `SwaggerEndpoint`，无需为接入统一响应增加控制器或测试接口。
