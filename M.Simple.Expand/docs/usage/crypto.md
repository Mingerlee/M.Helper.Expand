# 加解密与摘要

AES、RSA 加解密及签名。适用 M.Simple.Expand 1.0.1.8 / .NET 8。

## 可运行示例

```powershell
dotnet run --project docs/samples/M.Simple.Expand.Samples.csproj --configuration Release -- crypto
```

```csharp
using M.Simple.Expand;
using System.Security.Cryptography;

static partial class Demo
{
    public static void Crypto()
    {
        const string plain = "示例文本";
        var aes = plain.AesEncrypt();
        Check(aes.CipherText.AesDecrypt(aes.Key, aes.IV) == plain, "AES 往返");
        Console.WriteLine("AES 加解密验证通过（不输出密钥）");
        using var rsa = RSA.Create(2048);
        var publicKey = rsa.ToXmlString(false);
        var privateKey = rsa.ToXmlString(true);
        var cipher = plain.RsaEncrypt(publicKey);
        Check(cipher.RsaDecrypt(privateKey) == plain, "RSA 往返");
        var signature = plain.RsaSignData(privateKey);
        Check(plain.RsaVerifyData(signature, publicKey), "RSA 签名");
        var modernSignature = plain.RsaSignDataSha256(rsa.ExportPkcs8PrivateKeyPem());
        Check(plain.RsaVerifyDataSha256(modernSignature, rsa.ExportSubjectPublicKeyInfoPem()), "RSA SHA-256/PSS 签名");
        Check(Convert.FromBase64String(plain.Sha256Hash()).Length == 32, "SHA-256 摘要");
        Check(plain.Md5Hash() == plain.MD5Encrypt(), "MD5 历史别名兼容");
        Console.WriteLine("RSA 公钥加密、私钥解密与签名验证通过");
        Check(plain.MD5Verify(plain.MD5Encrypt()), "兼容摘要");
        Console.WriteLine("历史 MD5 摘要验证通过");
    }
}
```

示例与其他文件共享 Demo.Check 和辅助模型；完整上下文见[示例项目](../samples/README.md)。

## 新接口完整示例

以下示例覆盖全部新增入口，由上方 `crypto` 命令一起执行。`Check` 为示例项目中的断言方法；复制到自己的项目时可改为测试断言或业务判断。

```csharp
using M.Simple.Expand;
using System.Security.Cryptography;

static partial class Demo
{
    public static void ModernCryptography()
    {
        // 1. SHA-256摘要：UTF-8输入，Base64输出；不可解密。
        var hash = "abc".Sha256Hash();
        Check(hash == "ungWv48Bz+pBQUDeXa4iI7ADYaOWF3qctBD/YfIAFa0=", "SHA-256 标准摘要");
        Check("".Sha256Hash() == "47DEQpj8HBSa+/TImW+5JCeuQeRkm5NMpJWZG3hSuFU=", "空串摘要");
        Console.WriteLine($"SHA-256(abc)：{hash}");

        // 2. RSA SHA-256/PSS：临时生成演示密钥；实际使用时加载自己的密钥。
        using var rsa = RSA.Create(2048);
        var privatePem = rsa.ExportPkcs8PrivateKeyPem();
        var publicPem = rsa.ExportSubjectPublicKeyInfoPem();
        const string message = "订单编号：1001";
        var signature = message.RsaSignDataSha256(privatePem);
        bool valid = message.RsaVerifyDataSha256(signature, publicPem);
        bool tampered = "订单编号：1002".RsaVerifyDataSha256(signature, publicPem);
        Check(valid && !tampered, "PSS 验签及篡改检测");
        Console.WriteLine($"SHA-256/PSS 验签：{valid}；篡改后：{tampered}");

        // 3. 对接SHA-256/PKCS#1协议时，两端显式指定相同填充。
        var pkcs1Signature = message.RsaSignDataSha256(privatePem, RSASignaturePadding.Pkcs1);
        bool pkcs1Valid = message.RsaVerifyDataSha256(pkcs1Signature, publicPem, RSASignaturePadding.Pkcs1);
        Check(pkcs1Valid, "SHA-256/PKCS#1 验签");
        Console.WriteLine($"SHA-256/PKCS#1 验签：{pkcs1Valid}");

        // 4. 复用XML密钥也要使用新入口，旧RsaSignData仍固定为SHA-1。
        var xmlSignature = message.RsaSignDataSha256(rsa.ToXmlString(true));
        Check(message.RsaVerifyDataSha256(xmlSignature, rsa.ToXmlString(false)), "XML 密钥新协议");

        // 5. MD5准确命名别名：仅兼容历史摘要，不能替代密码存储算法。
        var legacyHash = "abc".Md5Hash();
        Check(legacyHash == "abc".MD5Encrypt() && "abc".MD5Verify(legacyHash), "MD5 别名与旧接口互通");
        var salted = message.Md5SaltHash();
        Check(message.MD5SaltVerify(salted.CipherText, salted.EncryptSecret), "自动盐摘要验证");
        // 使用已有盐复算历史摘要，无需重新生成盐。
        var recomputed = message.Md5SaltHash(salted.EncryptSecret);
        Check(recomputed == salted.CipherText, "指定盐摘要复算");
        Console.WriteLine("MD5 别名、自动盐及指定盐兼容验证通过（不输出盐和密钥）");
    }
}
```

运行时会输出标准 SHA-256 摘要、PSS 验签 `True`、篡改后 `False`、PKCS#1 验签 `True`，并验证MD5别名与旧接口互通。密钥、盐和签名不写入控制台。

RSA签名用于验证原文及签名方，不是把原文加密；验签仍需提供原文。保存签名时同时记录算法/填充版本和密钥标识，后续按对应协议验签。

## 使用规则与边界

### 新旧协议选择

| 用途 | 新协议入口 | 历史兼容入口 |
| --- | --- | --- |
| RSA签名 | RsaSignDataSha256 / RsaVerifyDataSha256，SHA-256，默认PSS | RsaSignData / RsaVerifyData，SHA-1与PKCS#1 v1.5 |
| 文本摘要 | Sha256Hash，UTF-8，Base64 | MD5Encrypt / MD5Verify，原有输出不变 |
| 历史MD5准确命名 | Md5Hash、Md5SaltHash（含指定盐和自动生成盐重载） | 分别委托MD5Encrypt、MD5SaltEncrypt，结果及空值约定一致 |

新签名入口支持XML及标准未加密PEM密钥（PKCS#1、PKCS#8私钥，PKCS#1、SubjectPublicKeyInfo公钥），要求RSA至少2048位。签名输出为Base64，不附带算法元数据；签名方和验签方必须选择相同填充。对接已有SHA-256/PKCS#1协议时双方显式传入 RSASignaturePadding.Pkcs1，不会自动回退到SHA-1或另一种填充。

Sha256Hash 对空串返回标准空消息摘要，null抛ArgumentNullException。Md5Hash 保留历史约定：null或空串返回空串。摘要不是加密，SHA-256和加盐MD5均不是新密码存储方案。

验签不匹配返回false；无效Base64、密钥、弱密钥和无效参数抛异常。PSS签名含随机性，同一内容重复签名不要求字节一致，应用应通过验签判断有效性。

### 迁移约定

- 旧签名、MD5及加盐MD5入口继续保持原有算法、编码、盐拼接和输出格式，不自动升级或重写历史数据。AES、RSA加解密及分段协议本次未改动。
- 新数据可单独记录协议版本（例如 rsa-sha256-pss、rsa-sha256-pkcs1、legacy-rsa-sha1），同时保留密钥标识。读取时按明确版本选择验签入口；未标记的历史数据按原协议处理，不按“试到成功”为规则降级。
- SHA-1签名不能直接变成SHA-256签名，MD5摘要不能还原原文后再转换SHA-256；有原始数据或用户再次提供原文时才能重新计算。先完成双方协议升级，再切换新写入入口。

### 其他边界

- AesEncrypt() 返回 CipherText、Key、IV；解密使用 cipher.AesDecrypt(key, iv)。示例生成临时密钥，仅打印往返验证结果。

- RSA 示例用 .NET RSA.Create 生成 XML 格式公私钥，使用公共扩展完成公钥加密、私钥解密及签名验签。RSAEncryptHelper 是 internal 类型，外部项目不能直接调用。无参 RsaEncrypt 封装内部使用 pair.PrivateKey 调用加密方法。

- RsaEncryptWithBlock、RsaDecryptWithBlock 用于原有分段协议，密钥格式与填充方式需和调用方协议匹配。

- MD5Encrypt、MD5SaltEncrypt 为不可逆历史摘要，不是可解密的加密。仅用于兼容已有协议，不作为新密码存储方案。

- AES 实现的模式、IV、RSA 填充与摘要算法以源码协议为准；本手册验证功能往返，不代表经过密码学审计。示例不输出密钥、私钥或凭证。

## API 查询

[API 与源码索引](../api.md)列出实际声明及 XML 注释。
