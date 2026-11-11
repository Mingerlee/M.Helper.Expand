# M.Simple.Expand 使用手册站点

本目录是静态 IIS 网站，端口 8091，默认文档 index.html。

- 地址：http://127.0.0.1:8091/
- 物理路径：D:/oym/M.Helper.Expand/M.Simple.Expand/website
- 网站名称：M.Simple.Expand.Docs
- 应用池：M.Simple.Expand.Docs.Static，无托管代码

在库目录运行 tools/Build-DocsSite.ps1 更新手册。首次挂载用管理员 Windows PowerShell 运行 tools/Install-DocsIis.ps1。

完整构建与部署说明见 ../docs/publishing.md。此目录可独立由 IIS 提供静态内容，服务器不需要运行 Node.js 或 .NET Web 服务。
