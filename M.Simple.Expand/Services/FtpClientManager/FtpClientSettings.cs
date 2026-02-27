namespace M.Simple.Expand.Services
{
    /// <summary>
    ///     <para>M.Simple.Services服务类</para>
    ///     <para>ftp配置</para>
    /// </summary>
    public class FtpClientSettings
    {
        /// <summary>
        ///     ftp服务地址
        /// </summary>
        public string FtpServerIP { get; set; }
        /// <summary>
        ///     ftp根目录（指定FTP连接成功后的当前目录, 如果不指定即默认为根目录）
        /// </summary>
        public string FtpRemotePath { get; set; } = string.Empty;
        /// <summary>
        ///     ftp用户名
        /// </summary>
        public string FtpUserID { get; set; }
        /// <summary>
        ///     ftp密码
        /// </summary>
        public string FtpPassword { get; set; }
        /// <summary>
        ///     ftpUrl
        /// </summary>
        public string FtpUrl
        {
            get
            {
                if (!string.IsNullOrEmpty(FtpRemotePath))
                {
                    return "ftp://" + FtpServerIP + "/" + FtpRemotePath + "/";
                }
                else
                {
                    return "ftp://" + FtpServerIP + "/";
                }
            }
        }
    }
}
