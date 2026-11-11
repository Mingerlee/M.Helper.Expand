using System.Net;
using System.Net.Sockets;
using System.Text;
using M.Simple.Expand.Services;
using M.Simple.Helper;

namespace M.Simple.Expand.Test;

[TestClass]
public class FtpDownloadTests
{
    [DataTestMethod]
    [DataRow("helper", "reject", true)]
    [DataRow("service", "reject", true)]
    [DataRow("async", "reject", true)]
    [DataRow("helper", "interrupt", true)]
    [DataRow("service", "interrupt", true)]
    [DataRow("async", "interrupt", true)]
    [DataRow("helper", "reject", false)]
    [DataRow("service", "reject", false)]
    [DataRow("async", "reject", false)]
    [DataRow("helper", "interrupt", false)]
    [DataRow("service", "interrupt", false)]
    [DataRow("async", "interrupt", false)]
    [DataRow("helper", "success", true)]
    [DataRow("service", "success", true)]
    [DataRow("async", "success", true)]
    [DataRow("helper", "success", false)]
    [DataRow("service", "success", false)]
    [DataRow("async", "success", false)]
    [DataRow("helper", "empty", true)]
    [DataRow("service", "empty", true)]
    [DataRow("async", "empty", true)]
    [DataRow("helper", "locked", true)]
    [DataRow("service", "locked", true)]
    [DataRow("async", "locked", true)]
    [Timeout(20000)]
    public async Task Download_PublishesOnlyCompletedTransfers(string entry, string mode, bool existing)
    {
        string directory = Path.Combine(Path.GetTempPath(), "ftp-regression-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string target = Path.Combine(directory, "result.bin");
        byte[] original = Encoding.UTF8.GetBytes("原有文件必须完整保留");
        try
        {
            if (existing) File.WriteAllBytes(target, original);
            using var server = new LocalFtpServer(mode == "locked" ? "success" : mode);
            if (mode == "reject" || mode == "interrupt" || mode == "locked")
            {
                using (var targetLock = mode == "locked"
                    ? new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.Read) : null)
                {
                    await Assert.ThrowsExceptionAsync<Exception>(() => DownloadAsync(entry, server.Address, directory));
                }
                Assert.AreEqual(existing, File.Exists(target));
                if (existing) CollectionAssert.AreEqual(original, File.ReadAllBytes(target));
            }
            else
            {
                await DownloadAsync(entry, server.Address, directory);
                CollectionAssert.AreEqual(mode == "empty" ? Array.Empty<byte>() : LocalFtpServer.Payload,
                    File.ReadAllBytes(target));
            }
            Assert.AreEqual(0, Directory.GetFiles(directory, ".ftp-*.tmp").Length);
            // 验证失败后没有遗留文件句柄。
            if (File.Exists(target))
            {
                using var exclusive = new FileStream(target, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            await server.Completion;
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    [Timeout(20000)]
    public async Task DownloadAsync_CancellationDuringTransferPreservesOriginal()
    {
        string directory = Path.Combine(Path.GetTempPath(), "ftp-regression-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string target = Path.Combine(directory, "result.bin");
        File.WriteAllText(target, "original");
        try
        {
            using var server = new LocalFtpServer("cancel");
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            Task download = DownloadAsync("async", server.Address, directory, cancellation.Token);
            await server.TransferStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
            cancellation.Cancel();
            await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => download);
            Assert.AreEqual("original", File.ReadAllText(target));
            Assert.AreEqual(0, Directory.GetFiles(directory, ".ftp-*.tmp").Length);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static Task DownloadAsync(string entry, string address, string directory, CancellationToken token = default)
    {
        if (entry == "helper")
            return Task.Run(() => new FtpClient(address, "test", "test").Download(directory, "result.bin"));
        var service = new FtpClientService(new FtpClientSettings
        {
            FtpServerIP = address,
            FtpUserID = "test",
            FtpPassword = "test"
        });
        return entry == "async" ? service.FtpDownloadAsync(directory, "result.bin", token)
            : Task.Run(() => service.FtpDownload(directory, "result.bin"));
    }

    // 仅监听回环地址的最小FTP测试端，模拟拒绝、部分传输失败与正常完成。
    private sealed class LocalFtpServer : IDisposable
    {
        public static readonly byte[] Payload = Enumerable.Range(0, 20000).Select(i => (byte)(i % 251)).ToArray();
        private readonly TcpListener listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource stop = new(TimeSpan.FromSeconds(15));
        public string Address { get; }
        public Task Completion { get; }
        public TaskCompletionSource<bool> TransferStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public LocalFtpServer(string mode)
        {
            listener.Start();
            Address = "127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port;
            Completion = RunAsync(mode);
        }

        private async Task RunAsync(string mode)
        {
            using var control = await listener.AcceptTcpClientAsync(stop.Token);
            using var reader = new StreamReader(control.GetStream(), Encoding.ASCII);
            using var writer = new StreamWriter(control.GetStream(), Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };
            TcpListener passive = null;
            try
            {
                await writer.WriteLineAsync("220 Local test FTP");
                while (await reader.ReadLineAsync(stop.Token) is string line)
                {
                    string command = line.Split(' ')[0];
                    switch (command)
                    {
                        case "USER": await writer.WriteLineAsync("331 Password required"); break;
                        case "PASS": await writer.WriteLineAsync("230 Logged in"); break;
                        case "PWD": await writer.WriteLineAsync("257 \"/\" is current directory"); break;
                        case "TYPE":
                        case "OPTS":
                        case "CWD": await writer.WriteLineAsync("200 OK"); break;
                        case "PASV":
                        case "EPSV":
                            passive = new TcpListener(IPAddress.Loopback, 0);
                            passive.Start();
                            int port = ((IPEndPoint)passive.LocalEndpoint).Port;
                            await writer.WriteLineAsync(command == "PASV"
                                ? $"227 Entering Passive Mode (127,0,0,1,{port / 256},{port % 256})"
                                : $"229 Entering Extended Passive Mode (|||{port}|)");
                            break;
                        case "SIZE": await writer.WriteLineAsync("213 " + Payload.Length); break;
                        case "RETR":
                            if (mode == "reject")
                            {
                                await writer.WriteLineAsync("550 File unavailable");
                                return;
                            }
                            await writer.WriteLineAsync("150 Opening binary data connection");
                            using (var data = await passive.AcceptTcpClientAsync(stop.Token))
                            {
                                if (mode != "empty")
                                    await data.GetStream().WriteAsync(Payload.AsMemory(0, mode == "success" ? Payload.Length : 1024), stop.Token);
                                TransferStarted.TrySetResult(true);
                                if (mode == "cancel") await Task.Delay(Timeout.Infinite, stop.Token);
                            }
                            await writer.WriteLineAsync(mode == "interrupt" ? "426 Transfer aborted" : "226 Transfer complete");
                            break;
                        case "QUIT": await writer.WriteLineAsync("221 Goodbye"); return;
                        default: throw new InvalidOperationException("Unexpected FTP command: " + command);
                    }
                }
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
            finally
            {
                passive?.Stop();
            }
        }

        public void Dispose()
        {
            stop.Cancel();
            listener.Stop();
            Completion.GetAwaiter().GetResult();
            stop.Dispose();
        }
    }
}
