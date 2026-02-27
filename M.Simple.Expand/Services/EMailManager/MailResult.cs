using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M.Simple.Expand.Services
{
    /// <summary>
    ///     <para>M.Simple.Services服务类</para>
    ///     <para>邮件发送接口模型</para>
    /// </summary>
    public class MailResult
    {
        /// <summary>
        ///     构造
        /// </summary>
        public MailResult()
        {
            Status = MailStatus.Success;
        }
        /// <summary>
        ///     
        /// </summary>
        /// <param name="msg"></param>
        public MailResult(string msg)
        {
            Status = MailStatus.Failure;
            ErrorMsg = msg;
        }

        /// <summary>
        /// 邮件发送状态 
        /// </summary>
        public MailStatus Status { get; set; }
        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMsg { get; set; }
    }
}
