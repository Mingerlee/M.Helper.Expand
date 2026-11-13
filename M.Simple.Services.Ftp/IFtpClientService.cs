namespace M.Simple.Services.Ftp
{
    /// <summary>
    ///     <para>M.Simple.Services服务类</para>
    ///     <para>ftp服务接口</para>
    /// </summary>
    public interface IFtpClientService
    {
        /// <summary>
        ///     用配置副本替换当前实例配置；不影响其他实例或已构造的请求
        /// </summary>
        /// <param name="settings"></param>
        void ResetFtpClientSettings(FtpClientSettings settings);
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        /// <para>ftp：上传文件到当前目录</para>
        /// </summary>
        /// <param name="filename">文件名(绝对路径)</param>
        bool FtpUpload(string filename);
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        /// <para>ftp：上传文件到当前目录</para>
        /// </summary>
        /// <param name="filename">文件名(绝对路径)</param>
        /// <param name="cancellationToken">取消网络等待和数据传输的令牌</param>
        /// <exception cref="OperationCanceledException">操作已取消，异常保留调用方令牌</exception>
        Task<bool> FtpUploadAsync(string filename, CancellationToken cancellationToken);
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        /// <para>ftp：下载文件</para>
        /// </summary>
        /// <param name="filePath">保存路径</param>
        /// <param name="fileName">文件名称</param>
        bool FtpDownload(string filePath, string fileName);
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        /// <para>ftp：下载文件</para>
        /// </summary>
        /// <param name="filePath">保存路径</param>
        /// <param name="fileName">文件名称</param>
        /// <param name="cancellationToken">取消网络等待和数据传输的令牌</param>
        /// <exception cref="OperationCanceledException">操作已取消，异常保留调用方令牌</exception>
        Task<bool> FtpDownloadAsync(string filePath, string fileName, CancellationToken cancellationToken);
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        ///  <para>ftp：删除当前目录下的指定文件</para>
        /// </summary>
        /// <param name="fileName">文件名</param>
        bool FtpDelete(string fileName);
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        ///  <para>ftp：删除当前目录下的指定文件</para>
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="cancellationToken">取消网络等待和数据传输的令牌</param>
        /// <exception cref="OperationCanceledException">操作已取消，异常保留调用方令牌</exception>
        Task<bool> FtpDeleteAsync(string fileName, CancellationToken cancellationToken);
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        /// <para>删除文件夹</para>
        /// </summary>
        /// <param name="folderName">文件夹名称</param>
        bool FtpRemoveDirectory(string folderName);
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        /// <para>删除文件夹</para>
        /// </summary>
        /// <param name="folderName">文件夹名称</param>
        /// <param name="cancellationToken">取消网络等待和数据传输的令牌</param>
        /// <exception cref="OperationCanceledException">操作已取消，异常保留调用方令牌</exception>
        Task<bool> FtpRemoveDirectoryAsync(string folderName, CancellationToken cancellationToken);
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        /// <para>获取当前目录下明细(包含文件和文件夹)</para>
        /// </summary>
        /// <returns></returns>
        string[] GetFtpListDirectoryDetails();
        /// <summary>
        /// <para>M.Simple.Services服务类</para>    
        /// <para>获取当前目录下文件列表(仅文件)</para>    
        /// </summary>
        /// <param name="mask">目标文件后缀名：eg:".xlsx,.xls"</param>
        /// <param name="fileReadingRules">目标文件读取规则：eg:"_cg,_sx,_kc，只读取文件名中包含这些的文件"</param>
        /// <returns></returns>
        List<string> GetFtpFileList(string mask = "*.*", string fileReadingRules = "");
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        ///<para> 获取当前目录下所有的文件夹列表(仅文件夹)</para>
        /// </summary>
        /// <returns></returns>
        string[] GetFtpDirectoryList();
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        ///<para> 判断当前目录下指定的子目录是否存在</para>
        /// </summary>
        /// <param name="RemoteDirectoryName">指定的目录名</param>
        bool FtpDirectoryExist(string RemoteDirectoryName);
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        /// <para>判断当前目录下指定的文件是否存在</para>
        /// </summary>
        /// <param name="RemoteFileName">远程文件名</param>
        bool FtpFileExist(string RemoteFileName);
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        /// <para>创建文件夹</para>
        /// </summary>
        /// <param name="dirName"></param>
        bool FtpMakeDirectory(string dirName);
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        /// <para>获取指定文件大小</para>
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        long GetFtpFileSize(string filename);
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        /// <para>重命名</para>
        /// </summary>
        /// <param name="currentFilename">当前文件名</param>
        /// <param name="newFilename">新文件名</param>
        bool FtpReName(string currentFilename, string newFilename);
        /// <summary>
        /// <para>M.Simple.Services服务类</para>
        /// <para>切换当前目录</para>
        /// </summary>
        /// <param name="DirectoryName"></param>
        /// <param name="IsRoot">true 绝对路径   false 相对路径</param>
        void GotoFtpDirectory(string DirectoryName, bool IsRoot);
    }
}
