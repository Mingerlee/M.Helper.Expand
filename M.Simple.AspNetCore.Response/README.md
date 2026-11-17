# M.Simple.AspNetCore.Response

面向 .NET 8 / ASP.NET Core MVC 控制器的统一 JSON 响应包。独立于 M.Simple.Expand，无额外第三方运行时依赖；通过 ASP.NET Core FrameworkReference 使用宿主共享框架。

## 安装与接入

```powershell
dotnet add package M.Simple.AspNetCore.Response --version 1.0.0.1
```

```csharp
using M.Simple.AspNetCore.Response;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddUnifiedResponse(); // 内部已调用 AddControllers，也支持宿主已有的 MVC 注册。

var app = builder.Build();
app.UseUnifiedResponse(); // 放在认证、授权和控制器执行之前；尽量靠近 Build() 之后。
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

控制器正常返回即可，无需继承基类或手动包装：

```csharp
[HttpGet]
public IActionResult Get() => Ok(new { name = "example" });
```

```json
{
  "code": 200,
  "message": "成功",
  "data": { "name": "example" }
}
```

安装命令要求相应版本已发布到配置的包源。源码构建及本地安装见下文。

## 默认规则

- code 与实际 HTTP 状态一致，保留 201/400/401/403/404/409/500 等状态；Created 的 Location 头保留。
- ObjectResult、JsonResult、普通 DTO、数组和空结果自动包装；`NoContent()` 和其他 204 结果转换为 HTTP 200 以承载 JSON，空结果 data 为 null。
- `[ApiController]` 参数校验及 JSON 解析失败返回 HTTP 400，message 为“参数校验失败”，data 为字段名到错误数组的字典。
- 未处理异常记录完整日志，返回 HTTP 500、“服务器内部错误”和 null 数据；不返回异常详情。
- 空错误响应由状态码中间件统一，例如路由不存在的 404、认证中间件产生的空 401/403；已有响应体的中间件错误不重写。
- `ProblemDetails` 转为状态消息；`ValidationProblemDetails` 保留 Errors，其他详情不暴露。
- code、message、data 使用固定 JSON 字段名；data 为 null 时也输出。其他业务字段遵循宿主的 MVC System.Text.Json 配置。
- 文件、重定向、显式 ContentResult、304 保留原协议语义；HEAD 不输出响应体。已开始发送的响应、流式输出不重新包装。
- 本包面向 MVC 控制器，不自动包装 Minimal API、Razor Pages、SSE 或其他中间件已经写出的内容。

参数校验失败示例：

```json
{
  "code": 400,
  "message": "参数校验失败",
  "data": { "Name": ["The Name field is required."] }
}
```

## 自定义消息、手动返回与跳过

```csharp
builder.Services.AddUnifiedResponse(options =>
{
    options.SuccessMessage = "OK";
    options.ValidationErrorMessage = "参数有误";
    options.ExceptionMessage = "服务暂时不可用";
    options.StatusMessageFactory = code => code == 404 ? "未找到记录" : null;
});
```

已返回 `ApiResponse<T>` 的结果不重复包装。手动返回时保持 code 与 HTTP 状态一致：

```csharp
return BadRequest(new ApiResponse<object?>(400, "业务校验失败", null));
```

在操作或控制器上加 `[SkipUnifiedResponse]`，可保留原来的返回方式和标准校验格式。该特性同时跳过包的异常包装；未处理异常保留空 500，尚未记录的异常由包记录日志。自定义特定错误可在控制器返回适当的状态及数据，或在本包之前注册自定义 IExceptionHandler。

不再重复注册其他全局响应过滤器或异常处理中间件。MVC 全局行为按最终 Options 配置执行；如果宿主在 AddUnifiedResponse 后再次覆盖 InvalidModelStateResponseFactory，将采用宿主的版本。AddUnifiedResponse 和 UseUnifiedResponse 可重复调用，但通常只需各调用一次。

## 与 M.Simple.Logging 配合，自动去重异常日志

统一响应包 **1.0.1** 与 M.Simple.Logging **1.0.1.5** 起支持协作去重，两个包仍各自独立，不互相依赖。

```csharp
using M.Simple.Logging;
using M.Simple.AspNetCore.Response;

builder.Services.AddSimpleLogging();
builder.Services.AddUnifiedResponse();

var app = builder.Build();
app.UseUnifiedResponse();
app.UseSimpleLoggingRequestContext(); // 保持默认 LogUnhandledExceptions=true。
app.UseAuthorization();
app.MapControllers();
```

默认情况下，请求日志中间件先记录一条包含脱敏请求参数的异常日志，再将异常向外传播；统一响应包识别该异常已记录，负责返回统一的 500 响应。无需关闭 LogUnhandledExceptions。若单独使用响应包，或者请求采集被排除、关闭或日志分类被过滤，响应包仍会尝试记录尚未记录的异常，但不会补采被排除的参数。

- 去重范围为“同一个 HTTP 请求中的同一个异常实例”，保留不同请求的错误和业务主动写入的 LogError。
- 默认推荐上述顺序；参数采集中间件放在响应包外层时，也可由响应包写入带参数的单条异常日志。
- 使用包自己的异常边界调用 IExceptionHandler，避免 .NET 8 内置 ExceptionHandlerMiddleware 再额外生成异常日志；不通过全局关闭框架日志实现去重。
- 不额外叠加其他异常日志中间件。宿主自定义处理器、业务主动日志，以及已开始发送响应后重新抛给服务器的异常，仍按各自规则记录。
- 跨包通过 HttpContext.Items 的保留键 `M.Simple.UnhandledException.LoggedException` 传递已记录的异常引用，不修改异常对象，不跨请求共享状态。

## 可选 Swagger

安装同版本 `M.Simple.AspNetCore.Response.Swagger`，参考该目录 README。核心包不依赖 Swashbuckle。

## 源码构建与测试

在仓库根目录执行：

```powershell
dotnet test M.Simple.AspNetCore.Response.Test/M.Simple.AspNetCore.Response.Test.csproj -c Release
dotnet pack M.Simple.AspNetCore.Response/M.Simple.AspNetCore.Response.csproj -c Release -o artifacts/nuget
dotnet pack M.Simple.AspNetCore.Response.Swagger/M.Simple.AspNetCore.Response.Swagger.csproj -c Release -o artifacts/nuget
```

把 artifacts/nuget 添加为本地 NuGet 源即可安装；对外发布需另行推送到 NuGet。源码项目包版本为 1.0.1。
