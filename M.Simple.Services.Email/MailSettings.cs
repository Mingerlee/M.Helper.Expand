using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace M.Simple.Services.Email
{
    /// <summary>
    ///     <para>M.Simple.Services服务类</para>
    ///     <para>邮件服务配置</para>
    /// </summary>
    public class MailSettings
    {
        /// <summary>
        ///     smtp主机服务器
        /// </summary>
        [Required]
        public string StmpHostServer { get; set; }
        /// <summary>
        ///     用户名
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        ///     密码
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        ///     发件人邮箱地址
        /// </summary>
        [Required]
        public string FromAddress { get; set; }
        /// <summary>
        ///     端口
        /// </summary>
        public int PortNumber { get; set; }
        /// <summary>
        ///     是否启用SSL验证
        /// </summary>
        public bool EnableSSL { get; set; }
        /// <summary>
        ///     验证授权
        /// </summary>
        public bool RequireAuth { get; set; }
        /// <summary>
        ///    邮件发送选项
        /// </summary>
        public MailDeliveryOptions DeliveryOption { get; set; }
        /// <summary>
        ///     指定目录路径
        /// </summary>
        public string SpecifiedPickUpDirectoryPath { get; set; }
        /// <summary>
        ///     获得凭证
        /// </summary>
        public NetworkCredential GetCredential => new(Username, Password);
    }
}
