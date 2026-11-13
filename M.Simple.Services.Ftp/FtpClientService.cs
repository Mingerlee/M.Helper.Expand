using System.Net;
using System.Text;

namespace M.Simple.Services.Ftp
{
    /// <summary>
    ///     <para>M.Simple.Services服务类</para>
    ///     <para>ftp服务</para>
    /// </summary>
    public class FtpClientService : IFtpClientService
    {
        private readonly object _settingsLock = new();
        private FtpClientSettings _settings = null!;
        /// <summary>
        ///     ftp服务配置快照；修改后需通过ResetFtpClientSettings应用
        /// </summary>
        public FtpClientSettings Settings
        {
            get
            {
                lock (_settingsLock) return CopySettings(_settings);
            }
            internal set
            {
                ArgumentNullException.ThrowIfNull(value);
                lock (_settingsLock) _settings = CopySettings(value);
            }
        }

        private static FtpClientSettings CopySettings(FtpClientSettings settings)
        {
            // 只复制配置值，服务内部的目录修改不回写容器或调用方持有的配置。
            return new FtpClientSettings
            {
                FtpServerIP = settings.FtpServerIP,
                FtpRemotePath = settings.FtpRemotePath,
                FtpUserID = settings.FtpUserID,
                FtpPassword = settings.FtpPassword
            };
        }
        /// <summary>
        ///     构造
        /// </summary>
        /// <param name="settings"></param>
        public FtpClientService(FtpClientSettings settings)
        {
            Settings = settings;
        }
        /// <summary>
        ///     用配置副本替换当前实例配置；不影响其他实例或已构造的请求
        /// </summary>
        /// <param name="settings"></param>
        public void ResetFtpClientSettings(FtpClientSettings settings)
        {
            Settings = settings;
        }
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        /// <para>ftp：上传文件到当前目录</para>
        /// </summary>
        /// <param name="filename">文件名(绝对路径)</param>
        public bool FtpUpload(string filename)
        {
            FileInfo fileInf = new(filename);

            FtpWebRequest reqFTP = CreateFtpWebRequest(fileInf.Name);
            reqFTP.Method = WebRequestMethods.Ftp.UploadFile;
            reqFTP.UseBinary = true;
            reqFTP.UsePassive = false;
            reqFTP.ContentLength = fileInf.Length;
            int buffLength = 2048;
            byte[] buff = new byte[buffLength];
            int contentLen;
            using FileStream fs = fileInf.OpenRead();
            try
            {
                using Stream strm = reqFTP.GetRequestStream();
                contentLen = fs.Read(buff, 0, buffLength);
                while (contentLen != 0)
                {
                    strm.Write(buff, 0, contentLen);
                    contentLen = fs.Read(buff, 0, buffLength);
                }
                strm.Close();
                fs.Close();
                // 读取最终响应，确认上传完成并释放响应资源。
                using var uploadResponse = reqFTP.GetResponse();
                reqFTP = null;
                return true;
            }
            catch (Exception ex)
            {
                // 协议错误也可能附带响应，必须释放其连接。
                if (ex is WebException webException) webException.Response?.Dispose();
                throw new Exception("Ftp Upload Error --> " + ex.Message);
            }
        }
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        /// <para>ftp：上传文件到当前目录</para>
        /// </summary>
        /// <param name="filename">文件名(绝对路径)</param>
        /// <param name="cancellationToken">取消网络等待和数据传输的令牌</param>
        /// <exception cref="OperationCanceledException">操作已取消，异常保留调用方令牌</exception>
        public async Task<bool> FtpUploadAsync(string filename, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                FileInfo fileInf = new(filename);
                FtpWebRequest reqFTP = CreateFtpWebRequest(fileInf.Name);
                reqFTP.Method = WebRequestMethods.Ftp.UploadFile;
                reqFTP.UseBinary = true;
                reqFTP.UsePassive = false;
                reqFTP.ContentLength = fileInf.Length;
                using var cancellationRegistration = cancellationToken.Register(reqFTP.Abort);
                await using FileStream fs = new(filename, FileMode.Open, FileAccess.Read, FileShare.Read,
                    81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
                // 获取请求流、复制数据和等待最终确认均走异步网络路径。
                await using (Stream strm = await reqFTP.GetRequestStreamAsync().ConfigureAwait(false))
                {
                    await fs.CopyToAsync(strm, 81920, cancellationToken).ConfigureAwait(false);
                }
                using WebResponse response = await reqFTP.GetResponseAsync().ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (cancellationToken.IsCancellationRequested)
            {
                // Abort可能产生WebException，取消仍以调用方的token向上传播。
                if (ex is WebException webException) webException.Response?.Dispose();
                throw new OperationCanceledException("FTP操作已取消。", ex, cancellationToken);
            }
            catch (Exception ex)
            {
                if (ex is WebException webException) webException.Response?.Dispose();
                throw new Exception("Ftp Upload Error --> " + ex.Message, ex);
            }
        }
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        /// <para>ftp：下载文件</para>
        /// </summary>
        /// <param name="filePath">保存路径</param>
        /// <param name="fileName">文件名称</param>
        public bool FtpDownload(string filePath, string fileName)
        {
            string? temporaryPath = null;
            try
            {
                if (!Directory.Exists(filePath))
                    Directory.CreateDirectory(filePath);

                string targetPath = Path.GetFullPath(Path.Combine(filePath, fileName));
                // 临时文件与目标文件位于同一目录，下载成功前不打开或清空目标文件。
                temporaryPath = Path.Combine(Path.GetDirectoryName(targetPath)!, ".ftp-" + Guid.NewGuid().ToString("N") + ".tmp");
                FtpWebRequest reqFTP = CreateFtpWebRequest(fileName);
                reqFTP.Method = WebRequestMethods.Ftp.DownloadFile;
                using (FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse())
                using (Stream ftpStream = response.GetResponseStream())
                using (FileStream outputStream = new(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    ftpStream.CopyTo(outputStream);
                    outputStream.Flush();
                }

                // 完整传输并成功关闭流后才替换；替换失败也不主动删除原文件。
                File.Move(temporaryPath, targetPath, true);
                return true;
            }
            catch (Exception ex)
            {
                // 协议错误也可能附带响应，必须释放其连接。
                if (ex is WebException webException) webException.Response?.Dispose();
                throw new Exception("Ftp Download Error --> " + ex.Message, ex);
            }
            finally
            {
                // 清理未完成的下载；清理失败不覆盖原始下载异常。
                if (temporaryPath != null)
                {
                    try { File.Delete(temporaryPath); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
        }
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        /// <para>ftp：下载文件</para>
        /// </summary>
        /// <param name="filePath">保存路径</param>
        /// <param name="fileName">文件名称</param>
        /// <param name="cancellationToken">取消网络等待和数据传输的令牌</param>
        /// <exception cref="OperationCanceledException">操作已取消，异常保留调用方令牌</exception>
        public async Task<bool> FtpDownloadAsync(string filePath, string fileName, CancellationToken cancellationToken)
        {
            string? temporaryPath = null;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!Directory.Exists(filePath))
                    Directory.CreateDirectory(filePath);

                string targetPath = Path.GetFullPath(Path.Combine(filePath, fileName));
                // 临时文件与目标文件位于同一目录，下载成功前不打开或清空目标文件。
                temporaryPath = Path.Combine(Path.GetDirectoryName(targetPath)!, ".ftp-" + Guid.NewGuid().ToString("N") + ".tmp");
                FtpWebRequest reqFTP = CreateFtpWebRequest(fileName);
                reqFTP.Method = WebRequestMethods.Ftp.DownloadFile;
                // 取消时中断网络请求，同时保护已有目标文件。
                using var cancellationRegistration = cancellationToken.Register(reqFTP.Abort);
                using (FtpWebResponse response = (FtpWebResponse)await reqFTP.GetResponseAsync().ConfigureAwait(false))
                using (Stream ftpStream = response.GetResponseStream())
                await using (FileStream outputStream = new(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                    81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
                {
                    await ftpStream.CopyToAsync(outputStream, 81920, cancellationToken).ConfigureAwait(false);
                    await outputStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }

                // 完整传输并成功关闭流后才替换；替换失败也不主动删除原文件。
                cancellationToken.ThrowIfCancellationRequested();
                File.Move(temporaryPath, targetPath, true);
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (cancellationToken.IsCancellationRequested)
            {
                // Abort可能产生WebException，取消仍以调用方的token向上传播。
                if (ex is WebException webException) webException.Response?.Dispose();
                throw new OperationCanceledException("FTP操作已取消。", ex, cancellationToken);
            }
            catch (Exception ex)
            {
                if (ex is WebException webException) webException.Response?.Dispose();
                throw new Exception("Ftp Download Error --> " + ex.Message, ex);
            }
            finally
            {
                // 清理未完成的下载；清理失败不覆盖原始下载异常。
                if (temporaryPath != null)
                {
                    try { File.Delete(temporaryPath); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
        }
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        ///  <para>ftp：删除当前目录下的指定文件</para>
        /// </summary>
        /// <param name="fileName">文件名</param>
        public bool FtpDelete(string fileName)
        {
            try
            {
                FtpWebRequest reqFTP = CreateFtpWebRequest(fileName);
                reqFTP.Method = WebRequestMethods.Ftp.DeleteFile;
                reqFTP.UsePassive = false;

                string result = string.Empty;
                using FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                long size = response.ContentLength;
                using Stream datastream = response.GetResponseStream();
                using StreamReader sr = new(datastream);
                result = sr.ReadToEnd();
                sr.Close();
                datastream.Close();
                response.Close();
                reqFTP = null;
                return true;
            }
            catch (Exception ex)
            {
                // 协议错误也可能附带响应，必须释放其连接。
                if (ex is WebException webException) webException.Response?.Dispose();
                throw new Exception("Ftp Delete Error --> " + ex.Message + "  fileName:" + fileName);
            }
        }
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        ///  <para>ftp：删除当前目录下的指定文件</para>
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="cancellationToken">取消网络等待和数据传输的令牌</param>
        /// <exception cref="OperationCanceledException">操作已取消，异常保留调用方令牌</exception>
        public async Task<bool> FtpDeleteAsync(string fileName, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                FtpWebRequest reqFTP = CreateFtpWebRequest(fileName);
                reqFTP.Method = WebRequestMethods.Ftp.DeleteFile;
                reqFTP.UsePassive = false;
                // 命令响应没有数据正文；异步等待状态确认，取消时直接中断请求。
                using var cancellationRegistration = cancellationToken.Register(reqFTP.Abort);
                using WebResponse response = await reqFTP.GetResponseAsync().ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (cancellationToken.IsCancellationRequested)
            {
                // Abort可能产生WebException，取消仍以调用方的token向上传播。
                if (ex is WebException webException) webException.Response?.Dispose();
                throw new OperationCanceledException("FTP操作已取消。", ex, cancellationToken);
            }
            catch (Exception ex)
            {
                if (ex is WebException webException) webException.Response?.Dispose();
                throw new Exception("Ftp Delete Error --> " + ex.Message + "  fileName:" + fileName, ex);
            }
        }
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        /// <para>删除文件夹</para>
        /// </summary>
        /// <param name="folderName">文件夹名称</param>
        public bool FtpRemoveDirectory(string folderName)
        {
            try
            {
                FtpWebRequest reqFTP = CreateFtpWebRequest(folderName);
                reqFTP.Method = WebRequestMethods.Ftp.RemoveDirectory;
                reqFTP.UsePassive = false;

                string result = string.Empty;
                using FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                long size = response.ContentLength;
                using Stream datastream = response.GetResponseStream();
                using StreamReader sr = new(datastream);
                result = sr.ReadToEnd();
                sr.Close();
                datastream.Close();
                response.Close();
                reqFTP = null;
                return true;
            }
            catch (Exception ex)
            {
                // 协议错误也可能附带响应，必须释放其连接。
                if (ex is WebException webException) webException.Response?.Dispose();
                throw new Exception("Ftp Remove Directory Error --> " + ex.Message + "  folderName:" + folderName);
            }
        }
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        /// <para>删除文件夹</para>
        /// </summary>
        /// <param name="folderName">文件夹名称</param>
        /// <param name="cancellationToken">取消网络等待和数据传输的令牌</param>
        /// <exception cref="OperationCanceledException">操作已取消，异常保留调用方令牌</exception>
        public async Task<bool> FtpRemoveDirectoryAsync(string folderName, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                FtpWebRequest reqFTP = CreateFtpWebRequest(folderName);
                reqFTP.Method = WebRequestMethods.Ftp.RemoveDirectory;
                reqFTP.UsePassive = false;
                // 命令响应没有数据正文；异步等待状态确认，取消时直接中断请求。
                using var cancellationRegistration = cancellationToken.Register(reqFTP.Abort);
                using WebResponse response = await reqFTP.GetResponseAsync().ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (cancellationToken.IsCancellationRequested)
            {
                // Abort可能产生WebException，取消仍以调用方的token向上传播。
                if (ex is WebException webException) webException.Response?.Dispose();
                throw new OperationCanceledException("FTP操作已取消。", ex, cancellationToken);
            }
            catch (Exception ex)
            {
                if (ex is WebException webException) webException.Response?.Dispose();
                throw new Exception("Ftp Remove Directory Error --> " + ex.Message + "  folderName:" + folderName, ex);
            }
        }
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        /// <para>获取当前目录下明细(包含文件和文件夹)</para>
        /// </summary>
        /// <returns></returns>
        public string[] GetFtpListDirectoryDetails()
        {
            try
            {
                StringBuilder result = new();
                FtpWebRequest reqFTP = CreateFtpWebRequest();
                reqFTP.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                reqFTP.UsePassive = false;
                using WebResponse response = reqFTP.GetResponse();
                using StreamReader reader = new(response.GetResponseStream(), Encoding.Default);

                string line = reader.ReadLine();

                while (line != null)
                {
                    result.Append(line);
                    result.Append('\n');
                    line = reader.ReadLine();
                }
                if (result.Length == 0) return Array.Empty<string>();
                result.Remove(result.Length - 1, 1);
                reader.Close();
                response.Close();
                reqFTP = null;
                return result.ToString().Split('\n');
            }
            catch (Exception ex)
            {
                // 协议错误也可能附带响应，必须释放其连接。
                if (ex is WebException webException) webException.Response?.Dispose();
                throw new Exception("Ftp Get List Directory Details Error --> " + ex.Message);
            }
        }
        /// <summary>
        /// <para>M.Simple.Services服务类</para>    
        /// <para>获取当前目录下文件列表(仅文件)</para>    
        /// </summary>
        /// <param name="mask">目标文件后缀名：eg:".xlsx,.xls"</param>
        /// <param name="fileReadingRules">目标文件读取规则：eg:"_cg,_sx,_kc，只读取文件名中包含这些的文件"</param>
        /// <returns></returns>
        public List<string> GetFtpFileList(string mask = "*.*", string fileReadingRules = "")
        {
            List<string> result = [];
            try
            {
                FtpWebRequest reqFTP = CreateFtpWebRequest();
                reqFTP.Method = WebRequestMethods.Ftp.ListDirectory;
                using WebResponse response = reqFTP.GetResponse();
                using StreamReader reader = new(response.GetResponseStream(), Encoding.Default);

                string[] masks = mask.Split(',');
                string[] fileRules = fileReadingRules.ToUpper().Split(',');
                string line = reader.ReadLine();
                while (line != null)
                {
                    if (!string.IsNullOrEmpty(fileReadingRules))
                    {
                        //如果不符合命名规则的，直接跳过
                        if (fileRules != null && !fileRules.Any(a => line.Contains(a, StringComparison.OrdinalIgnoreCase)))
                        {
                            line = reader.ReadLine();
                            continue;
                        }
                    }

                    if (mask.Trim() != string.Empty && mask.Trim() != "*.*")
                    {
                        foreach (string targetMask in masks)
                        {
                            //判断当前文件和目标文件后缀名是否一致
                            if (line.EndsWith(targetMask, StringComparison.OrdinalIgnoreCase))
                            {
                                result.Add(line);
                                break;
                            }
                        }
                    }
                    else
                    {
                        result.Add(line);
                    }
                    line = reader.ReadLine();
                }
                reader.Close();
                response.Close();
                return result;
            }
            catch (Exception ex)
            {
                // 协议错误也可能附带响应，必须释放其连接。
                if (ex is WebException webException) webException.Response?.Dispose();
                if (ex.Message.Trim() != "远程服务器返回错误: (550) 文件不可用(例如，未找到文件，无法访问文件)。")
                {
                    throw new Exception("Ftp GetFileList Error --> " + ex.Message.ToString());
                }
                return [];
            }
        }
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        ///<para> 获取当前目录下所有的文件夹列表(仅文件夹)</para>
        /// </summary>
        /// <returns></returns>
        public string[] GetFtpDirectoryList()
        {
            string[] drectory = GetFtpListDirectoryDetails();
            string m = string.Empty;
            foreach (string str in drectory)
            {
                int dirPos = str.IndexOf("<DIR>");
                if (dirPos > 0)
                {
                    /*判断 Windows 风格*/
                    m += str.Substring(dirPos + 5).Trim() + "\n";
                }
                else if (str.Trim().StartsWith("D", StringComparison.OrdinalIgnoreCase))
                {
                    /*判断 Unix 风格*/
                    string dir = str.Substring(54).Trim();
                    if (dir != "." && dir != "..")
                    {
                        m += dir + "\n";
                    }
                }
            }

            char[] n = ['\n'];
            return m.Split(n);
        }
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        ///<para> 判断当前目录下指定的子目录是否存在</para>
        /// </summary>
        /// <param name="RemoteDirectoryName">指定的目录名</param>
        public bool FtpDirectoryExist(string RemoteDirectoryName)
        {
            string[] dirList = GetFtpDirectoryList();
            foreach (string str in dirList)
            {
                if (str.Trim() == RemoteDirectoryName.Trim())
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        /// <para>判断当前目录下指定的文件是否存在</para>
        /// </summary>
        /// <param name="RemoteFileName">远程文件名</param>
        public bool FtpFileExist(string RemoteFileName)
        {
            var fileList = GetFtpFileList("*.*");
            foreach (string str in fileList)
            {
                if (str.Trim() == RemoteFileName.Trim())
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        /// <para>创建文件夹</para>
        /// </summary>
        /// <param name="dirName"></param>
        public bool FtpMakeDirectory(string dirName)
        {
            try
            {
                FtpWebRequest reqFTP = CreateFtpWebRequest(dirName);
                reqFTP.Method = WebRequestMethods.Ftp.MakeDirectory;
                reqFTP.UseBinary = true;
                reqFTP.UsePassive = false;
                using FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                using Stream ftpStream = response.GetResponseStream();

                ftpStream.Close();
                response.Close();
                reqFTP = null;
                return true;
            }
            catch (Exception ex)
            {
                // 协议错误也可能附带响应，必须释放其连接。
                if (ex is WebException webException) webException.Response?.Dispose();
                throw new Exception("FtpHelper MakeDir Error --> " + ex.Message);
            }
        }
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        /// <para>获取指定文件大小</para>
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public long GetFtpFileSize(string filename)
        {
            try
            {
                FtpWebRequest reqFTP = CreateFtpWebRequest(filename);
                reqFTP.Method = WebRequestMethods.Ftp.GetFileSize;
                reqFTP.UseBinary = true;
                reqFTP.UsePassive = false;
                using FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                using Stream ftpStream = response.GetResponseStream();
                long fileSize = response.ContentLength;

                ftpStream.Close();
                response.Close();
                reqFTP = null;
                return fileSize;
            }
            catch (Exception ex)
            {
                // 协议错误也可能附带响应，必须释放其连接。
                if (ex is WebException webException) webException.Response?.Dispose();
                throw new Exception("Ftp GetFileSize Error --> " + ex.Message);
            }
        }
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        /// <para>重命名</para>
        /// </summary>
        /// <param name="currentFilename">当前文件名</param>
        /// <param name="newFilename">新文件名</param>
        public bool FtpReName(string currentFilename, string newFilename)
        {
            try
            {
                FtpWebRequest reqFTP = CreateFtpWebRequest(currentFilename);
                reqFTP.Method = WebRequestMethods.Ftp.Rename;
                reqFTP.RenameTo = newFilename;
                reqFTP.UseBinary = true;
                reqFTP.UsePassive = false;
                using FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                using Stream ftpStream = response.GetResponseStream();

                ftpStream.Close();
                response.Close();
                reqFTP = null;

                return true;
            }
            catch (Exception ex)
            {
                // 协议错误也可能附带响应，必须释放其连接。
                if (ex is WebException webException) webException.Response?.Dispose();
                throw new Exception("Ftp ReName Error --> " + ex.Message);
            }
        }
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        /// <para>切换当前实例目录；并行的目录操作流程应分别使用独立实例</para>
        /// </summary>
        /// <param name="DirectoryName"></param>
        /// <param name="IsRoot">true 绝对路径   false 相对路径</param>
        public void GotoFtpDirectory(string DirectoryName, bool IsRoot)
        {
            ArgumentNullException.ThrowIfNull(DirectoryName);
            lock (_settingsLock)
            {
                // 相对目录只插入一个分隔符，目录状态仅属于当前服务实例。
                string nextPath = DirectoryName.Trim('/');
                string currentPath = _settings.FtpRemotePath?.Trim('/') ?? string.Empty;
                _settings.FtpRemotePath = IsRoot || currentPath.Length == 0 ? nextPath
                    : nextPath.Length == 0 ? currentPath : currentPath + "/" + nextPath;
            }
        }

        private FtpWebRequest CreateFtpWebRequest(string fileName = "")
        {
            // 同一个请求的地址与认证来自同一快照，重置只影响后续构造的请求。
            var settings = Settings;
            var url = string.Concat(settings.FtpUrl, fileName);
            FtpWebRequest reqFTP = (FtpWebRequest)WebRequest.Create(new Uri(url));
            reqFTP.Credentials = new NetworkCredential(settings.FtpUserID, settings.FtpPassword);
            reqFTP.KeepAlive = false; // 操作完成后关闭连接

            return reqFTP;
        }
    }
}
