# 构建文档与发布到本地 IIS

## 站点配置

| 项目 | 值 |
|---|---|
| IIS 网站名称 | M.Simple.Expand.Docs |
| 地址 | http://127.0.0.1:8091/ |
| 物理目录 | D:/oym/M.Helper.Expand/M.Simple.Expand/website |
| 应用池 | M.Simple.Expand.Docs.Static，无托管代码 |
| 绑定 | 127.0.0.1:8091，HTTP |

此站点独立于 8090 的 M.Simple 手册。仅绑定回环地址，用于本机阅读。

## 重新生成

在库目录运行，要求 .NET 8 SDK、Node.js 14+、Windows PowerShell：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/Build-DocsSite.ps1
```

脚本构建原库、执行 15 个演示场景、保存实际输出，从当前源码生成 API 索引，并生成 HTML、搜索索引和包下载。构建日志及 IIS 结果保存到 artifacts。手写说明在 docs/usage 下，站点生成器不覆盖手写说明。

## 首次挂载

使用管理员 Windows PowerShell：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/Install-DocsIis.ps1
```

脚本只配置该指定站点、专用应用池与目录读取权限。若同名站点的路径或绑定不同，或端口已被其他 IIS 站点占用，则停止而不覆盖。安装 IIS 网站需要 Windows 管理员权限；代码和静态页面生成不需要管理员权限。

如 IIS 未启用静态内容或默认文档，先由管理员启用相应组件。web.config 配置默认文档及 .nupkg 下载类型。现有站点指向 website，重新生成文件后即可访问更新内容。

## 验证

首页、各功能页、搜索资源、源码展示下载和 NuGet 包均应返回 HTTP 200。示例输出代表库的本地执行验证，不代表真实邮件、FTP 或业务系统集成验证。
