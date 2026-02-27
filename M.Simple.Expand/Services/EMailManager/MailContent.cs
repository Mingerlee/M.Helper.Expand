namespace M.Simple.Expand.Services
{
    /// <summary>
    ///     <para>M.Simple.Services服务类</para>    
    ///     <para>邮件内容</para>    
    /// </summary>
    public class MailContent
    {
        /// <summary>
        /// 收件人邮件地址
        /// </summary>
        public string ToAddresses { get; set; }
        /// <summary>
        /// 抄送地址
        /// </summary>
        public string CcAddresses { get; set; }
        /// <summary>
        /// 密送地址
        /// </summary>
        public string BccAddresses { get; set; }
        /// <summary>
        ///     ReplyToAddresses
        /// </summary>
        public string ReplyToAddresses { get; set; }
        /// <summary>
        /// 主题
        /// </summary>
        public string Subject { get; set; }
        /// <summary>
        /// 内容
        /// </summary>
        public string Body { get; set; }
        /// <summary>
        /// 是否Html格式
        /// </summary>
        public bool IsHTML { get; set; } = false;
        /// <summary>
        /// 附件
        /// </summary>
        public List<string> AttachmentPaths { get; set; } = [];
    }
}
