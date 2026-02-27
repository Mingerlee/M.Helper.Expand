using System.Net;
using System.Text;

namespace M.Simple.Expand.Services
{
    /// <summary>
    ///     <para>M.Simple.Services服务类</para>
    ///     <para>ftp服务</para>
    /// </summary>
    public class FtpClientService : IFtpClientService
    {
        /// <summary>
        ///     ftp服务配置
        /// </summary>
        public FtpClientSettings Settings { get; internal set; }
        /// <summary>
        ///     构造
        /// </summary>
        /// <param name="settings"></param>
        public FtpClientService(FtpClientSettings settings)
        {
            Settings = settings;
        }
        /// <summary>
        ///     重新设置ftp服务配置
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
            FileStream fs = fileInf.OpenRead();
            try
            {
                Stream strm = reqFTP.GetRequestStream();
                contentLen = fs.Read(buff, 0, buffLength);
                while (contentLen != 0)
                {
                    strm.Write(buff, 0, contentLen);
                    contentLen = fs.Read(buff, 0, buffLength);
                }
                strm.Close();
                fs.Close();
                reqFTP = null;
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Ftp Upload Error --> " + ex.Message);
            }
        }
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        /// <para>ftp：上传文件到当前目录</para>
        /// </summary>
        /// <param name="filename">文件名(绝对路径)</param>
        /// <param name="cancellationToken"></param>
        public async Task<bool> FtpUploadAsync(string filename, CancellationToken cancellationToken)
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
            FileStream fs = fileInf.OpenRead();
            try
            {
                Stream strm = reqFTP.GetRequestStream();
                contentLen = await fs.ReadAsync(buff.AsMemory(0, buffLength), cancellationToken: cancellationToken);
                while (contentLen != 0)
                {
                    await strm.WriteAsync(buff.AsMemory(0, contentLen), cancellationToken: cancellationToken);
                    contentLen = await fs.ReadAsync(buff.AsMemory(0, buffLength), cancellationToken: cancellationToken);
                }
                strm.Close();
                fs.Close();
                reqFTP = null;
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Ftp Upload Error --> " + ex.Message);
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
            try
            {
                if (!Directory.Exists(filePath))
                    Directory.CreateDirectory(filePath);

                FileStream outputStream = new(filePath + "\\" + fileName, FileMode.Create);

                FtpWebRequest reqFTP = CreateFtpWebRequest(fileName);
                reqFTP.Method = WebRequestMethods.Ftp.DownloadFile;
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                Stream ftpStream = response.GetResponseStream();
                long cl = response.ContentLength;
                int bufferSize = 2048;
                int readCount;
                byte[] buffer = new byte[bufferSize];

                readCount = ftpStream.Read(buffer, 0, bufferSize);
                while (readCount > 0)
                {
                    outputStream.Write(buffer, 0, readCount);
                    readCount = ftpStream.Read(buffer, 0, bufferSize);
                }

                ftpStream.Close();
                outputStream.Close();
                response.Close();
                reqFTP = null;
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Ftp Download Error --> " + ex.Message);
            }
        }
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        /// <para>ftp：下载文件</para>
        /// </summary>
        /// <param name="filePath">保存路径</param>
        /// <param name="fileName">文件名称</param>
        /// <param name="cancellationToken"></param>
        public async Task<bool> FtpDownloadAsync(string filePath, string fileName, CancellationToken cancellationToken)
        {
            try
            {
                if (!Directory.Exists(filePath))
                    Directory.CreateDirectory(filePath);

                FileStream outputStream = new(filePath + "\\" + fileName, FileMode.Create);

                FtpWebRequest reqFTP = CreateFtpWebRequest(fileName);
                reqFTP.Method = WebRequestMethods.Ftp.DownloadFile;
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                Stream ftpStream = response.GetResponseStream();
                long cl = response.ContentLength;
                int bufferSize = 2048;
                int readCount;
                byte[] buffer = new byte[bufferSize];

                readCount = await ftpStream.ReadAsync(buffer.AsMemory(0, bufferSize), cancellationToken);
                while (readCount > 0)
                {
                    await outputStream.WriteAsync(buffer.AsMemory(0, readCount), cancellationToken);
                    readCount = await ftpStream.ReadAsync(buffer.AsMemory(0, bufferSize), cancellationToken);
                }

                ftpStream.Close();
                outputStream.Close();
                response.Close();
                reqFTP = null;
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Ftp Download Error --> " + ex.Message);
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
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                long size = response.ContentLength;
                Stream datastream = response.GetResponseStream();
                StreamReader sr = new(datastream);
                result = sr.ReadToEnd();
                sr.Close();
                datastream.Close();
                response.Close();
                reqFTP = null;
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Ftp Delete Error --> " + ex.Message + "  fileName:" + fileName);
            }
        }
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        ///  <para>ftp：删除当前目录下的指定文件</para>
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="cancellationToken"></param>
        public async Task<bool> FtpDeleteAsync(string fileName, CancellationToken cancellationToken)
        {
            try
            {
                FtpWebRequest reqFTP = CreateFtpWebRequest(fileName);
                reqFTP.Method = WebRequestMethods.Ftp.DeleteFile;
                reqFTP.UsePassive = false;

                string result = string.Empty;
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                long size = response.ContentLength;
                Stream datastream = response.GetResponseStream();
                StreamReader sr = new(datastream);
                result = await sr.ReadToEndAsync(cancellationToken);
                sr.Close();
                datastream.Close();
                response.Close();
                reqFTP = null;
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Ftp Delete Error --> " + ex.Message + "  fileName:" + fileName);
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
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                long size = response.ContentLength;
                Stream datastream = response.GetResponseStream();
                StreamReader sr = new(datastream);
                result = sr.ReadToEnd();
                sr.Close();
                datastream.Close();
                response.Close();
                reqFTP = null;
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Ftp Remove Directory Error --> " + ex.Message + "  folderName:" + folderName);
            }
        }
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        /// <para>删除文件夹</para>
        /// </summary>
        /// <param name="folderName">文件夹名称</param>
        /// <param name="cancellationToken"></param>
        public async Task<bool> FtpRemoveDirectoryAsync(string folderName, CancellationToken cancellationToken)
        {
            try
            {
                FtpWebRequest reqFTP = CreateFtpWebRequest(folderName);
                reqFTP.Method = WebRequestMethods.Ftp.RemoveDirectory;
                reqFTP.UsePassive = false;

                string result = string.Empty;
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                long size = response.ContentLength;
                Stream datastream = response.GetResponseStream();
                StreamReader sr = new(datastream);
                result = await sr.ReadToEndAsync(cancellationToken);
                sr.Close();
                datastream.Close();
                response.Close();
                reqFTP = null;
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Ftp Remove Directory Error --> " + ex.Message + "  folderName:" + folderName);
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
                WebResponse response = reqFTP.GetResponse();
                StreamReader reader = new(response.GetResponseStream(), Encoding.Default);

                string line = reader.ReadLine();

                while (line != null)
                {
                    result.Append(line);
                    result.Append('\n');
                    line = reader.ReadLine();
                }
                result.Remove(result.ToString().LastIndexOf('\n'), 1);
                reader.Close();
                response.Close();
                reqFTP = null;
                return result.ToString().Split('\n');
            }
            catch (Exception ex)
            {
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
                WebResponse response = reqFTP.GetResponse();
                StreamReader reader = new(response.GetResponseStream(), Encoding.Default);

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
                else if (str.Trim().Substring(0, 1).ToUpper() == "D")
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
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                Stream ftpStream = response.GetResponseStream();

                ftpStream.Close();
                response.Close();
                reqFTP = null;
                return true;
            }
            catch (Exception ex)
            {
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
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                Stream ftpStream = response.GetResponseStream();
                long fileSize = response.ContentLength;

                ftpStream.Close();
                response.Close();
                reqFTP = null;
                return fileSize;
            }
            catch (Exception ex)
            {
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
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                Stream ftpStream = response.GetResponseStream();

                ftpStream.Close();
                response.Close();
                reqFTP = null;

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Ftp ReName Error --> " + ex.Message);
            }
        }
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        /// <para>切换当前目录</para>
        /// </summary>
        /// <param name="DirectoryName"></param>
        /// <param name="IsRoot">true 绝对路径   false 相对路径</param>
        public void GotoFtpDirectory(string DirectoryName, bool IsRoot)
        {
            if (IsRoot)
            {
                Settings.FtpRemotePath = DirectoryName;
            }
            else
            {
                Settings.FtpRemotePath += DirectoryName + "/";
            }
        }

        private FtpWebRequest CreateFtpWebRequest(string fileName = "")
        {
            var url = string.Concat(Settings.FtpUrl, fileName);
            FtpWebRequest reqFTP = (FtpWebRequest)WebRequest.Create(new Uri(url));
            reqFTP.Credentials = new NetworkCredential(Settings.FtpUserID, Settings.FtpPassword);
            reqFTP.KeepAlive = false; // 操作完成后关闭连接

            return reqFTP;
        }
    }
}
