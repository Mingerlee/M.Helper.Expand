# M.Simple.AspNetCore.Response 接入与使用

统一 MVC 控制器 JSON 响应为 code、message、data。核心包无需 Swagger 依赖；若需要同步文档模型，参考 Swagger 适配接入页。

当前项目版本 `1.0.0.2`，目标框架 .NET 8。

## 安装包

在使用方项目目录执行，或通过 --project 指定项目：

```powershell
dotnet add package M.Simple.AspNetCore.Response --version 1.0.0.2
```

包源需包含该版本。安装包不会自动执行注册；保留项目原有配置，无需添加示例接口。

## Program.cs 配置

```csharp
using M.Simple.AspNetCore.Response; // 本次新增：引入本包接入所需扩展或类型。

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddUnifiedResponse(); // 本次新增：注册统一响应包装、参数校验和异常处理服务。

var app = builder.Build();

app.UseUnifiedResponse(); // 本次新增：启用异常及空错误响应处理，置于认证、授权和接口执行之前。

app.UseAuthorization();
app.MapControllers();
app.Run();
```

## 使用说明

正常控制器直接返回实体、DTO 或 Ok(value)，无需手动包装。

```json
{ "code": 200, "message": "成功", "data": { "id": 4 } }
```

[Swagger 文档适配](response-swagger.md)需另行安装并调用 AddUnifiedResponseSwagger()。

## 默认规则

- code 与实际 HTTP 状态一致，保留 201/400/401/403/404/409/500 等状态；Created 的 Location 头保留。
- ObjectResult、JsonResult、普通 DTO、数组和空结果自动包装；`NoContent()` 和其他 204 结果转换为 HTTP 200 以承载 JSON，空结果 data 为 null。
- `[ApiController]` 参数校验及 JSON 解析失败返回 HTTP 400，message 为“参数校验失败”，data 为字段名到错误数组的字典。
- SimpleException 业务异常返回 HTTP 400 和指定提示；其他未处理异常记录完整日志，返回 HTTP 500、“服务器内部错误”和 null 数据；不返回异常详情。
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


## 业务自定义异常 SimpleException.Of（1.0.0.2 起）

在已有控制器或业务方法中抛出允许展示给用户的提示，不需要为示例增加新的 HTTP 接口：

```csharp
using M.Simple.AspNetCore.Response;

// 放在现有方法的参数校验位置。
if (earlyWarningId <= 0)
    throw SimpleException.Of("预警编号必须大于零");
```

统一返回 HTTP 400，响应保持原有三个字段，不返回业务错误码：

```json
{
  "code": 400,
  "message": "预警编号必须大于零",
  "data": null
}
```

也可以由业务项目定义枚举，通过 Description 获取提示：

```csharp
using System.ComponentModel;

public enum ErrorCodeEnum
{
    [Description("预警编号必须大于零")]
    C0004,

    [Description("预警记录不存在")]
    A0006
}
```

在原有业务方法中使用：

```csharp
if (earlyWarningId <= 0)
    throw SimpleException.Of(ErrorCodeEnum.C0004);

if (earlyWarning is null)
    throw SimpleException.Of(ErrorCodeEnum.A0006);
```

未设置 Description 时使用枚举成员名；未定义的枚举值、null 或空白提示会被拒绝。字符串重载直接使用文本，不解析 nameof(ErrorCodeEnum.C0004) 的枚举描述。

SimpleException 当前仍按既有机制记录 Error 日志，与 M.Simple.Logging 配合时继续按同请求、同异常实例去重。只有主动抛出的 SimpleException 使用指定提示；普通系统异常继续返回 HTTP 500 和“服务器内部错误”，不暴露内部详情。标记 SkipUnifiedResponse 的端点继续跳过此异常包装。

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



## 接入注意事项

仅 AddUnifiedResponseSwagger 不会修改实际响应。文件、重定向、显式文本和流式结果保留协议语义；不自动包装 Minimal API。

## 已有控制器中的实际返回示例

以下代码分别放在现有控制器方法中，根据业务结果选择一个返回，不需要新增 HTTP 接口：

```csharp
return Ok(product);
```

product 为已有业务对象；统一响应会自动将它放入 data。业务校验失败可以继续返回原有 HTTP 错误结果：

```csharp
return BadRequest(new { reason = "商品状态不允许修改" });
```

返回 HTTP 400，code 为 400，业务对象放入 data；message 使用该状态的默认消息。需要自定义顶层 message 时手动返回 ApiResponse，过滤器不会再次包装：

```csharp
return BadRequest(new ApiResponse<object?>(400, "商品状态不允许修改", null));
```

ApiResponse 位于 M.Simple.AspNetCore.Response。使用现有客户端或 Swagger 执行原接口，检查 HTTP 状态和 code 一致，并核对 data；异常处理示例无需人为新增抛错接口。
