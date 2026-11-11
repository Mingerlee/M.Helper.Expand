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
        Console.WriteLine("RSA 公钥加密、私钥解密与签名验证通过");
        Check(plain.MD5Verify(plain.MD5Encrypt()), "兼容摘要");
        Console.WriteLine("历史 MD5 摘要验证通过");
    }
}
```

示例与其他文件共享 Demo.Check 和辅助模型；完整上下文见[示例项目](../samples/README.md)。

## 使用规则与边界

- AesEncrypt() 返回 CipherText、Key、IV；解密使用 cipher.AesDecrypt(key, iv)。示例生成临时密钥，仅打印往返验证结果。

- RSA 示例用 .NET RSA.Create 生成 XML 格式公私钥，使用公共扩展完成公钥加密、私钥解密及签名验签。RSAEncryptHelper 是 internal 类型，外部项目不能直接调用。无参 RsaEncrypt 封装内部使用 pair.PrivateKey 调用加密方法。

- RsaEncryptWithBlock、RsaDecryptWithBlock 用于原有分段协议，密钥格式与填充方式需和调用方协议匹配。

- MD5Encrypt、MD5SaltEncrypt 为不可逆历史摘要，不是可解密的加密。仅用于兼容已有协议，不作为新密码存储方案。

- AES 实现的模式、IV、RSA 填充与摘要算法以源码协议为准；本手册验证功能往返，不代表经过密码学审计。示例不输出密钥、私钥或凭证。

## API 查询

[API 与源码索引](../api.md)列出实际声明及 XML 注释。
