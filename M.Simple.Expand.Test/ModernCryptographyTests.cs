using System.Security.Cryptography;
using System.Text;

namespace M.Simple.Expand.Test;

[TestClass]
public class ModernCryptographyTests
{
    [DataTestMethod]
    [DataRow("", "47DEQpj8HBSa+/TImW+5JCeuQeRkm5NMpJWZG3hSuFU=")]
    [DataRow("abc", "ungWv48Bz+pBQUDeXa4iI7ADYaOWF3qctBD/YfIAFa0=")]
    public void Sha256Hash_MatchesStandardVectors(string text, string expected)
    {
        Assert.AreEqual(expected, text.Sha256Hash());
    }

    [TestMethod]
    public void Hashes_PreserveLegacyEmptyAndSaltSemantics()
    {
        Assert.AreEqual("kAFQmDzST7DWlj99KOF/cg==", "abc".MD5Encrypt());
        Assert.AreEqual("kAFQmDzST7DWlj99KOF/cg==", "abc".Md5Hash());
        Assert.AreEqual("", "".Md5Hash());
        Assert.AreEqual("", ((string)null).Md5Hash());
        Assert.ThrowsException<ArgumentNullException>(() => ((string)null).Sha256Hash());
        const string text = "兼容文本";
        const string salt = "fixed-test-salt";
        string expected = Convert.ToBase64String(MD5.HashData(Encoding.UTF8.GetBytes(text + salt)));
        Assert.AreEqual(expected, text.MD5SaltEncrypt(salt));
        Assert.AreEqual(expected, text.Md5SaltHash(salt));
        Assert.IsTrue(text.MD5SaltVerify(expected, salt));
        var generated = text.Md5SaltHash();
        Assert.IsTrue(text.MD5SaltVerify(generated.CipherText, generated.EncryptSecret));
        Assert.AreEqual(Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(text))), text.Sha256Hash());
    }

    [DataTestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void Sha256Signatures_InteroperateWithDotNetAndRejectTampering(bool pem, bool pkcs1)
    {
        using var rsa = RSA.Create(2048);
        using var other = RSA.Create(2048);
        string privateKey = pem ? rsa.ExportPkcs8PrivateKeyPem() : rsa.ToXmlString(true);
        string publicKey = pem ? rsa.ExportSubjectPublicKeyInfoPem() : rsa.ToXmlString(false);
        var padding = pkcs1 ? RSASignaturePadding.Pkcs1 : RSASignaturePadding.Pss;
        const string text = "现代签名\nUTF-8内容";
        byte[] data = Encoding.UTF8.GetBytes(text);
        string signature = text.RsaSignDataSha256(privateKey, padding);
        Assert.IsTrue(rsa.VerifyData(data, Convert.FromBase64String(signature), HashAlgorithmName.SHA256, padding));
        string standard = Convert.ToBase64String(rsa.SignData(data, HashAlgorithmName.SHA256, padding));
        Assert.IsTrue(text.RsaVerifyDataSha256(standard, publicKey, padding));
        Assert.IsFalse((text + "changed").RsaVerifyDataSha256(signature, publicKey, padding));
        Assert.IsFalse(text.RsaVerifyDataSha256(signature, other.ExportSubjectPublicKeyInfoPem(), padding));
        Assert.IsFalse(text.RsaVerifyDataSha256(signature, publicKey, pkcs1 ? RSASignaturePadding.Pss : RSASignaturePadding.Pkcs1));
        byte[] modified = Convert.FromBase64String(signature);
        modified[modified.Length - 1] ^= 1;
        Assert.IsFalse(text.RsaVerifyDataSha256(Convert.ToBase64String(modified), publicKey, padding));
    }

    [TestMethod]
    public void DefaultSignatureUsesPssAndSupportsEmptyMessageAndPkcs1PemKeys()
    {
        using var rsa = RSA.Create(2048);
        string signature = "".RsaSignDataSha256(rsa.ExportRSAPrivateKeyPem());
        Assert.IsTrue(rsa.VerifyData(Array.Empty<byte>(), Convert.FromBase64String(signature), HashAlgorithmName.SHA256, RSASignaturePadding.Pss));
        Assert.IsTrue("".RsaVerifyDataSha256(signature, rsa.ExportRSAPublicKeyPem()));
    }

    [TestMethod]
    public void LegacySha1ProtocolRemainsUnchangedAndDoesNotAutoUpgrade()
    {
        using var rsa = RSA.Create(2048);
        string privateKey = rsa.ToXmlString(true);
        string publicKey = rsa.ToXmlString(false);
        byte[] data = Encoding.UTF8.GetBytes("历史签名");
        string historical = Convert.ToBase64String(rsa.SignData(data, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1));
        Assert.AreEqual(historical, "历史签名".RsaSignData(privateKey));
        Assert.IsTrue("历史签名".RsaVerifyData(historical, publicKey));
        Assert.IsFalse("历史签名".RsaVerifyDataSha256(historical, publicKey, RSASignaturePadding.Pkcs1));
        string modern = "历史签名".RsaSignDataSha256(privateKey, RSASignaturePadding.Pkcs1);
        Assert.IsFalse("历史签名".RsaVerifyData(modern, publicKey));
    }

    [TestMethod]
    public void ModernSigningRejectsWeakKeysAndInvalidInputs()
    {
        using var weak = RSA.Create(1024);
        using var rsa = RSA.Create(2048);
        Assert.ThrowsException<CryptographicException>(() => "text".RsaSignDataSha256(weak.ToXmlString(true)));
        Assert.ThrowsException<CryptographicException>(() => "text".RsaVerifyDataSha256("AA==", weak.ToXmlString(false)));
        Assert.ThrowsException<CryptographicException>(() => "text".RsaSignDataSha256(rsa.ExportSubjectPublicKeyInfoPem()));
        Assert.ThrowsException<FormatException>(() => "text".RsaVerifyDataSha256("invalid base64", rsa.ExportSubjectPublicKeyInfoPem()));
        Assert.ThrowsException<ArgumentNullException>(() => ((string)null).RsaSignDataSha256(rsa.ToXmlString(true)));
        Assert.ThrowsException<ArgumentException>(() => "text".RsaSignDataSha256(""));
    }
}
