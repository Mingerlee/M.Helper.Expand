using System.Net;
using System.Net.Sockets;
using System.Text;
using M.Simple.Expand.Services;

namespace M.Simple.Expand.Test;

[TestClass]
public class FtpAsyncCancellationTests
{
    [DataTestMethod]
    [DataRow("upload", "greeting")]
    [DataRow("download", "greeting")]
    [DataRow("delete", "greeting")]
    [DataRow("remove", "greeting")]
    [DataRow("upload", "command")]
    [DataRow("download", "command")]
    [DataRow("delete", "command")]
    [DataRow("remove", "command")]
    [DataRow("upload", "final")]
    [DataRow("download", "final")]
    [DataRow("upload", "transfer")]
    [DataRow("download", "transfer")]
    [Timeout(20000)]
    public async Task PendingNetworkOperation_ReturnsTaskAndCanBeCancelled(string operation, string stage)
    {
        string directory = CreateDirectory();
        using var server = new DelayedFtpServer(stage);
        using var cancellation = new CancellationTokenSource();
        try
        {
            var service = CreateService(server.Address);
            // 专用线程只用于检测方法调用是否立即返回Task；服务器在指定阶段由信号阻塞。
            Task<Task<bool>> invocation = Task.Factory.StartNew(() => Invoke(service, operation, directory, cancellation.Token),
                CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            await server.StageReached.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Task<bool> pending = await invocation.WaitAsync(TimeSpan.FromSeconds(3));
            Assert.IsFalse(pending.IsCompleted, "服务器尚未响应，不应提前完成。");
            cancellation.Cancel();
            try
            {
                await pending.WaitAsync(TimeSpan.FromSeconds(3));
                Assert.Fail("取消应通过OperationCanceledException返回。");
            }
            catch (OperationCanceledException error)
            {
                Assert.AreEqual(cancellation.Token, error.CancellationToken);
            }
            Assert.IsTrue(pending.IsCanceled);
            Assert.AreEqual("original", File.ReadAllText(Path.Combine(directory, "target.bin")));
            Assert.AreEqual(0, Directory.GetFiles(directory, ".ftp-*.tmp").Length);
            using var exclusive = new FileStream(Path.Combine(directory, "source.bin"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            cancellation.Cancel();
            Directory.Delete(directory, true);
        }
    }

    [DataTestMethod]
    [DataRow("upload")]
    [DataRow("download")]
    [DataRow("delete")]
    [DataRow("remove")]
    public async Task PreCancelledToken_CancelsBeforeFileOrNetworkAccess(string operation)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var service = CreateService("127.0.0.1:1");
        string missing = Path.Combine(Path.GetTempPath(), "does-not-exist-" + Guid.NewGuid().ToString("N"));
        Task<bool> pending = Invoke(service, operation, missing, cancellation.Token);
        try { await pending; Assert.Fail("应优先处理取消。"); }
        catch (OperationCanceledException error) { Assert.AreEqual(cancellation.Token, error.CancellationToken); }
        Assert.IsTrue(pending.IsCanceled);
        Assert.IsFalse(Directory.Exists(missing));
    }

    [DataTestMethod]
    [DataRow("upload", false)]
    [DataRow("download", false)]
    [DataRow("delete", false)]
    [DataRow("remove", false)]
    [DataRow("upload", true)]
    [DataRow("download", true)]
    [DataRow("delete", true)]
    [DataRow("remove", true)]
    [Timeout(20000)]
    public async Task CompletionAndProtocolFailureRemainDistinctFromCancellation(string operation, bool fail)
    {
        string directory = CreateDirectory();
        using var server = new DelayedFtpServer(fail ? "reject" : "success");
        try
        {
            Task<bool> task = Invoke(CreateService(server.Address), operation, directory, CancellationToken.None);
            if (fail)
            {
                var error = await Assert.ThrowsExceptionAsync<Exception>(() => task);
                Assert.IsInstanceOfType(error.InnerException, typeof(WebException));
                Assert.IsFalse(task.IsCanceled);
            }
            else
            {
                Assert.IsTrue(await task.WaitAsync(TimeSpan.FromSeconds(5)));
                if (operation == "download") Assert.AreEqual("download", File.ReadAllText(Path.Combine(directory, "target.bin")));
            }
            using var exclusive = new FileStream(Path.Combine(directory, "source.bin"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Assert.AreEqual(0, Directory.GetFiles(directory, ".ftp-*.tmp").Length);
        }
        finally { Directory.Delete(directory, true); }
    }

    private static string CreateDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "ftp-async-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        // 大于发送缓冲区，确保上传中途取消测试确实存在待发送数据。
        File.WriteAllBytes(Path.Combine(directory, "source.bin"), new byte[8 * 1024 * 1024]);
        File.WriteAllText(Path.Combine(directory, "target.bin"), "original");
        return directory;
    }

    private static FtpClientService CreateService(string address) => new(new FtpClientSettings
    {
        FtpServerIP = address,
        FtpUserID = "test",
        FtpPassword = "test-only"
    });

    private static Task<bool> Invoke(FtpClientService service, string operation, string directory, CancellationToken token) => operation switch
    {
        "upload" => service.FtpUploadAsync(Path.Combine(directory, "source.bin"), token),
        "download" => service.FtpDownloadAsync(directory, "target.bin", token),
        "delete" => service.FtpDeleteAsync("target.bin", token),
        "remove" => service.FtpRemoveDirectoryAsync("folder", token),
        _ => throw new ArgumentException(operation)
    };

    // 回环FTP端点在协议阶段发出信号后暂停，避免用sleep猜测取消时机。
    private sealed class DelayedFtpServer : IDisposable
    {
        private readonly TcpListener listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource stop = new(TimeSpan.FromSeconds(15));
        private readonly Task completion;
        public string Address { get; }
        public TaskCompletionSource<bool> StageReached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public DelayedFtpServer(string stage)
        {
            listener.Start();
            Address = "127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port;
            completion = RunAsync(stage);
        }

        private async Task PauseAsync(string actual, string expected)
        {
            if (actual != expected) return;
            StageReached.TrySetResult(true);
            await Task.Delay(Timeout.Infinite, stop.Token);
        }

        private async Task RunAsync(string stage)
        {
            TcpListener passive = null;
            try
            {
                using var control = await listener.AcceptTcpClientAsync(stop.Token);
                using var reader = new StreamReader(control.GetStream(), Encoding.ASCII);
                using var writer = new StreamWriter(control.GetStream(), Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };
                await PauseAsync("greeting", stage);
                await writer.WriteLineAsync("220 Async test FTP");
                IPEndPoint active = null;
                while (await reader.ReadLineAsync(stop.Token) is string line)
                {
                    switch (line.Split(' ')[0])
                    {
                        case "USER": await writer.WriteLineAsync("331 Password required"); break;
                        case "PASS": await writer.WriteLineAsync("230 Logged in"); break;
                        case "PWD": await writer.WriteLineAsync("257 \"/\""); break;
                        case "TYPE":
                        case "OPTS":
                        case "CWD": await writer.WriteLineAsync("200 OK"); break;
                        case "PORT":
                            var parts = line.Substring(5).Split(',').Select(int.Parse).ToArray();
                            active = new IPEndPoint(IPAddress.Loopback, parts[4] * 256 + parts[5]);
                            await writer.WriteLineAsync("200 Active endpoint ready");
                            break;
                        case "PASV":
                            passive = new TcpListener(IPAddress.Loopback, 0);
                            passive.Start();
                            int port = ((IPEndPoint)passive.LocalEndpoint).Port;
                            await writer.WriteLineAsync($"227 Passive (127,0,0,1,{port / 256},{port % 256})");
                            break;
                        case "STOR":
                            await PauseAsync("command", stage);
                            if (stage == "reject") { await writer.WriteLineAsync("550 Upload denied"); break; }
                            using (var data = new TcpClient())
                            {
                                await data.ConnectAsync(active, stop.Token);
                                await writer.WriteLineAsync("150 Upload ready");
                                var buffer = new byte[1024];
                                await data.GetStream().ReadAsync(buffer, stop.Token);
                                await PauseAsync("transfer", stage);
                                await data.GetStream().CopyToAsync(Stream.Null, stop.Token);
                            }
                            await PauseAsync("final", stage);
                            await writer.WriteLineAsync("226 Upload complete");
                            break;
                        case "RETR":
                            await PauseAsync("command", stage);
                            if (stage == "reject") { await writer.WriteLineAsync("550 Download denied"); break; }
                            await writer.WriteLineAsync("150 Download ready");
                            using (var data = await passive.AcceptTcpClientAsync(stop.Token))
                            {
                                await data.GetStream().WriteAsync(Encoding.UTF8.GetBytes("download"), stop.Token);
                                await PauseAsync("transfer", stage);
                            }
                            await PauseAsync("final", stage);
                            await writer.WriteLineAsync("226 Download complete");
                            break;
                        case "DELE":
                        case "RMD":
                            await PauseAsync("command", stage);
                            await writer.WriteLineAsync(stage == "reject" ? "550 Command denied" : "250 Command complete");
                            break;
                        case "QUIT": await writer.WriteLineAsync("221 Goodbye"); return;
                        default: throw new InvalidOperationException("Unexpected command: " + line.Split(' ')[0]);
                    }
                }
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
            catch (IOException) { } // 客户端Abort关闭控制连接。
            finally { passive?.Stop(); }
        }

        public void Dispose()
        {
            stop.Cancel();
            listener.Stop();
            completion.GetAwaiter().GetResult();
            stop.Dispose();
        }
    }
}
