using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M.Helper.Expand
{
    /// <summary>
    ///     金额格式化枚举
    /// </summary>
    public enum FormatMoneyEnum
    {
        /// <summary>
        /// <para>eg:10000000.51=>10,000,001</para>
        /// <para>eg:10000000.49=>10,000,000</para>
        /// </summary>
        C0 = 101,
        /// <summary>
        /// <para>eg:10000000.51=>￥10,000,001</para>
        /// <para>eg:10000000.49=>￥10,000,000</para>
        /// </summary>
        C0_zh_CN = 102,
        /// <summary>
        /// <para>eg:10000000.51=>$10,000,001</para>
        /// <para>eg:10000000.49=>$10,000,000</para>
        /// </summary>
        C0_en_US = 103,
        /// <summary>
        ///  <para>eg:10000000.51=>10,000,000.5</para>
        ///  <para>eg:10000000.49=>10,000,000.5</para>
        /// </summary>
        C1,
        /// <summary>
        /// <para>eg:10000000.51=>￥10,000,000.5</para>
        /// <para>eg:10000000.49=>￥10,000,000.5</para>
        /// </summary>
        C1_zh_CN,
        /// <summary>
        ///     <para>eg:10000000.51=>$10,000,000.5</para>
        ///     <para>eg:10000000.49=>$10,000,000.5</para>
        /// </summary>
        C1_en_US,
        /// <summary>
        ///  <para>eg:10000000.515=>10,000,000.52</para>
        ///  <para>eg:10000000.511=>10,000,000.51</para>
        /// </summary>
        C2,
        /// <summary>
        /// <para>eg:10000000.515=>￥10,000,000.52</para>
        /// <para>eg:10000000.511=>￥10,000,000.51</para>
        /// </summary>
        C2_zh_CN,
        /// <summary>
        ///     <para>eg:10000000.515=>$10,000,000.52</para>
        ///     <para>eg:10000000.511=>$10,000,000.51</para>
        /// </summary>
        C2_en_US,
        /// <summary>
        ///  <para>eg:10000000.5151=>10,000,000.515</para>
        ///  <para>eg:10000000.5155=>10,000,000.516</para>
        /// </summary>
        C3,
        /// <summary>
        /// <para>eg:10000000.5151=>￥10,000,000.515</para>
        /// <para>eg:10000000.5155=>￥10,000,000.516</para>
        /// </summary>
        C3_zh_CN,
        /// <summary>
        ///     <para>eg:10000000.5151=>$10,000,000.515</para>
        ///     <para>eg:10000000.5155=>$10,000,000.516</para>
        /// </summary>
        C3_en_US,
        /// <summary>
        ///  <para>eg:10000000.51515=>10,000,000.5152</para>
        ///  <para>eg:10000000.51511=>10,000,000.5151</para>
        /// </summary>
        C4,
        /// <summary>
        /// <para>eg:10000000.51515=>￥10,000,000.5152</para>
        /// <para>eg:10000000.51511=>￥10,000,000.5151</para>
        /// </summary>
        C4_zh_CN,
        /// <summary>
        ///     <para>eg:10000000.51515=>$10,000,000.5152</para>
        ///     <para>eg:10000000.51511=>$10,000,000.5151</para>
        /// </summary>
        C4_en_US,

    }
}
