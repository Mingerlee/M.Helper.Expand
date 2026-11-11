# 实际运行结果

由文档构建脚本运行全部 15 个场景生成。示例含断言；未访问真实 SMTP、FTP 或业务数据库。

```text

[strings]
138****5678
userName
user_name
ABC123
cba

[chinese]
ZHONGGUO
ZG
2026年11月11日
农历二零二六年十月初三

[datetime]
2026/11/11
2026-11-01
2026-11-30
2026/11/12
相差天数：3

[money]
壹仟贰佰叁拾肆圆伍角陆分
¥1,234.56
四舍五入：1.24
向上取整：1.24

[collections]
分批：1,2 / 3,4 / 5
排除：1,3,5
末尾：4,5

[dictionary]
覆盖后 A=2
弹出 B=3

[enums]
已通过
审核通过
0: Pending
1: Approved

[datatable]
列数：2；行数：1
恢复：1 / 示例

[comparison]
Name: 旧名称 → 新名称
Amount: 100 → 120

[crypto]
AES 加解密验证通过（不输出密钥）
RSA 公钥加密、私钥解密与签名验证通过
历史 MD5 摘要验证通过

[configuration]
配置：M.Simple.Expand

[di]
服务装配成功

[services]
邮件与 FTP 服务解析成功；仅演示注册，不建立网络连接。

[identifiers]
GUID 格式与复用雪花生成器验证通过（ID 不作为固定输出展示）

[validation]
邮箱与手机号 DataAnnotations 校验通过

示例执行完成；未发送邮件、连接 FTP 或访问业务数据库。
```
