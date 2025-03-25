using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace M.Simple.Expand.Attributes
{
    /// <summary>
    ///     <para>M.Simple.Expand扩展</para>
    ///     <para>验证是否为电话号码格式：010-12345678</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public partial class IsTelephone : ValidationAttribute
    {
        /// <summary>
        ///     验证
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null) return null;

            if (!MyRegex().IsMatch(value.ToString() ?? "")) return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));

            return ValidationResult.Success;
        }

        [GeneratedRegex(@"^(\d{3,4}-)?\d{6,8}$")]
        private static partial Regex MyRegex();
    }
    /// <summary>
    ///     <para>M.Simple.Expand扩展</para>
    ///     <para>验证是否为手机号码格式：18888888888</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public partial class IsMobile : ValidationAttribute
    {
        /// <summary>
        ///     验证
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null) return null;

            if (!MyRegex().IsMatch(value.ToString() ?? "")) return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));

            return ValidationResult.Success;
        }

        [GeneratedRegex(@"^1[3456789]\d{9}$")]
        private static partial Regex MyRegex();
    }
    /// <summary>
    ///     <para>M.Simple.Expand扩展</para>
    ///     <para>正则表达式验证</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class RegexMatch : ValidationAttribute
    {
        /// <summary>
        ///     正则字符串
        /// </summary>
        public string RegexStr { get; set; }
        /// <summary>
        ///     验证
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null) return null;

            if (!Regex.IsMatch(value.ToString() ?? "", @$"{RegexStr}")) return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));

            return ValidationResult.Success;
        }
    }
    /// <summary>
    ///     <para>M.Simple.Expand扩展</para>
    ///     <para>验证是否为身份证号码格式：11010120250101223X</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class IsIDcard : ValidationAttribute
    {
        /// <summary>
        ///     验证
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null) return null;
            string? strValue = ((string)value).Substring(6, 4);
            try
            {
                if (DateTime.IsLeapYear(Convert.ToInt32(strValue)))
                {
                    string regexStr = @"(^[1-9]\d{5}(19|20)\d{2}((01|03|05|07|08|10|12)(0[1-9]|[1-2]\d|3[0-1])|(04|06|09|11)(0[1-9]|[1-2]\d|30)|02(0[1-9]|[1-2]\d))\d{3}[\dXx]$)";
                    if (!Regex.IsMatch(value.ToString() ?? "", regexStr)) return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
                }
                else
                {
                    string regexStr = @"(^[1-9]\d{5}(19|20)\d{2}((01|03|05|07|08|10|12)(0[1-9]|[1-2]\d|3[0-1])|(04|06|09|11)(0[1-9]|[1-2]\d|30)|02(0[1-9]|1\d|2[0-8]))\d{3}[\dXx]$)";
                    if (!Regex.IsMatch(value.ToString() ?? "", regexStr)) return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
                }
                return ValidationResult.Success;
            }
            catch
            {
                return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
            }
        }
    }
    /// <summary>
    ///     <para>M.Simple.Expand扩展</para>
    ///     <para>验证是否为电子邮件格式：test@example.com</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public partial class IsEmail : ValidationAttribute
    {
        /// <summary>
        ///     验证
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null) return null;

            if (!MyRegex().IsMatch(value.ToString() ?? "")) return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));

            return ValidationResult.Success;
        }

        [GeneratedRegex(@"^[A-Za-z0-9](([_\.\-]?[a-zA-Z0-9]+)*)@([A-Za-z0-9]+)(([\.\-]?[a-zA-Z0-9]+)*)\.([A-Za-z]{2,})$")]
        private static partial Regex MyRegex();
    }
    /// <summary>
    ///     <para>M.Simple.Expand扩展</para>
    ///     <para>验证是否为网址格式：https://www.baidu.com</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public partial class IsUrl : ValidationAttribute
    {
        /// <summary>
        ///     验证
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null) return null;

            if (!MyRegex().IsMatch(value.ToString() ?? "")) return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));

            return ValidationResult.Success;
        }

        [GeneratedRegex(@"^(((file|gopher|news|nntp|telnet|http|ftp|https|ftps|sftp)://)|(www\.))+(([a-zA-Z0-9\._-]+\.[a-zA-Z]{2,6})|([0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}))(/[a-zA-Z0-9\&%_\./-~-]*)?$")]
        private static partial Regex MyRegex();
    }
    /// <summary>
    ///     <para>M.Simple.Expand扩展</para>
    ///     <para>验证是否为日期格式</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class IsDateTime : ValidationAttribute
    {
        /// <summary>
        ///     验证
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            try
            {
                DateTime time = Convert.ToDateTime(value);
                return ValidationResult.Success;
            }
            catch
            {
                return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
            }
        }
    }
    /// <summary>
    ///     <para>M.Simple.Expand扩展</para>
    ///     <para>验证是否为Int类型</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public partial class IsInt : ValidationAttribute
    {
        /// <summary>
        ///     验证
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null) return null;
            try
            {
                if (!MyRegex().IsMatch(value.ToString() ?? "")) return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));

                if ((long.Parse(value.ToString() ?? "") > 0x7fffffffL) || (long.Parse(value.ToString() ?? "") < -2147483648L))
                {
                    return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
                }

                return ValidationResult.Success;
            }
            catch
            {
                return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
            }
        }

        [GeneratedRegex(@"^(-){0,1}\d+$")]
        private static partial Regex MyRegex();
    }
    /// <summary>
    ///     <para>M.Simple.Expand扩展</para>
    ///     <para>验证用户名格式：A123456_B</para>
    ///     <para>必须以字母开头，长度为4~19，可以包含字母、数字和_</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public partial class UserId : ValidationAttribute
    {
        /// <summary>
        ///     验证
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null) return null;
            try
            {
                if (!MyRegex().IsMatch(value.ToString() ?? "")) return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));

                return ValidationResult.Success;
            }
            catch
            {
                return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
            }
        }

        [GeneratedRegex(@"^[a-zA-Z]{1}([a-zA-Z0-9]|[_]){4,19}$")]
        private static partial Regex MyRegex();
    }
    /// <summary>
    ///     <para>M.Simple.Expand扩展</para>
    ///     <para>验证密码格式：</para>
    ///     <para>必须包含字母(大写或者小写)、数字和特殊符号，长度8~20</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public partial class Password : ValidationAttribute
    {
        /// <summary>
        ///     验证
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null) return null;
            try
            {
                if (!MyRegex().IsMatch(value.ToString() ?? "")) return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));

                return ValidationResult.Success;
            }
            catch
            {
                return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
            }
        }

        [GeneratedRegex(@"^(?=.*[0-9])(?=.*[a-zA-Z])(?=([\x21-\x7e]+)[^a-zA-Z0-9]).{8,20}$")]
        private static partial Regex MyRegex();
    }
    /// <summary>
    ///     <para>M.Simple.Expand扩展</para>
    ///     <para>大于最小值</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class GreaterThanMinValue : ValidationAttribute
    {
        /// <summary>
        ///     最小值
        /// </summary>
        public int MinValue { get; set; } = 0;
        /// <summary>
        ///     是否可以等于最小值
        /// </summary>
        public bool IsEqualMinValue { get; set; } = false;
        /// <summary>
        ///     验证
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            //为空返回提示
            if (value == null) return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
            try
            {
                _ = int.TryParse(value.ToString() ?? "", out int result);
                if (IsEqualMinValue && result < MinValue)
                {
                    return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
                }
                if (!IsEqualMinValue && result <= MinValue)
                {
                    return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
                }

                return ValidationResult.Success;
            }
            catch
            {
                return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
            }
        }
    }
    /// <summary>
    ///     <para>M.Simple.Expand扩展</para>
    ///     <para>小于最大值</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class LessThanMaxValue : ValidationAttribute
    {
        /// <summary>
        ///     最大值
        /// </summary>
        public int MaxValue { get; set; }
        /// <summary>
        ///     是否可以等于最大值
        /// </summary>
        public bool IsEqualMaxValue { get; set; } = false;
        /// <summary>
        ///     验证
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            //为空返回提示
            if (value == null) return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
            try
            {
                _ = int.TryParse(value.ToString() ?? "", out int result);

                if (IsEqualMaxValue && result > MaxValue)
                {
                    return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
                }

                if (!IsEqualMaxValue && result >= MaxValue)
                {
                    return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
                }

                return ValidationResult.Success;
            }
            catch
            {
                return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
            }
        }
    }
    /// <summary>
    ///     <para>M.Simple.Expand扩展</para>
    ///     <para>两者之间</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class BetweenValue : ValidationAttribute
    {
        /// <summary>
        ///     最小值
        /// </summary>
        public int MinValue { get; set; }
        /// <summary>
        ///     最大值
        /// </summary>
        public int MaxValue { get; set; }
        /// <summary>
        ///     是否可以等于最小值
        /// </summary>
        public bool IsEqualMinValue { get; set; } = false;
        /// <summary>
        ///     是否可以等于最大值
        /// </summary>
        public bool IsEqualMaxValue { get; set; } = false;
        /// <summary>
        ///     验证
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            //为空返回提示
            if (value == null) return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
            try
            {
                _ = int.TryParse(value.ToString() ?? "", out int result);

                if (IsEqualMinValue && !IsEqualMaxValue && !(result >= MinValue && result < MaxValue))
                {
                    return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
                }
                if (!IsEqualMinValue && IsEqualMaxValue && !(result > MinValue && result <= MaxValue))
                {
                    return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
                }
                if (IsEqualMaxValue && IsEqualMinValue && !(result >= MinValue && result <= MaxValue))
                {
                    return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
                }
                if (!IsEqualMaxValue && !IsEqualMinValue && !(result > MinValue && result < MaxValue))
                {
                    return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
                }

                return ValidationResult.Success;
            }
            catch
            {
                return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
            }
        }
    }
}
