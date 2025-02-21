using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using M.Helper.Expand.Helper;
using Microsoft.AspNetCore.Http;
using System;

namespace M.Helper.Expand
{
    /// <summary>
    /// 扩展类
    /// </summary>
    public static partial class Extensions
    {
        /// <summary>
        /// 获取客户Ip
        /// </summary>
        /// <param name="controllerBase"></param>
        /// <returns></returns>
        public static string GetClientIP(this ControllerBase controllerBase)
        {
            var ip = controllerBase.HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(ip))
            {
                return controllerBase.HttpContext.Connection.RemoteIpAddress.ToString().Replace("::1", "127.0.0.1");
            }
            return ip;
        }

        /// <summary>
        /// 获取 Claims Value
        /// </summary>
        /// <param name="claims"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetClaimsValue(this IEnumerable<Claim> claims, string type)
        {
            return claims.FirstOrDefault(t => t.Type == type)?.Value ?? "";
        }
        /// <summary>
        /// 获取用户 Token
        /// </summary>
        /// <returns></returns>
        public static T GetUserToken<T>(this IEnumerable<Claim> claims,T t) where T : class
        {
            var uid = claims.GetClaimsValue(ClaimTypes.Name);
            var email = claims.GetClaimsValue(ClaimTypes.NameIdentifier);

            //获取类型
            Type type = typeof(T);
            // 获取所有属性集合
            var properties = type.GetProperties();

            foreach (var propertie in properties)
            {
                if (propertie.Name.ToUpper() == "UID")
                {
                    propertie.SetValue(t, string.IsNullOrEmpty(uid) ? string.Empty : uid);
                }
                else if (propertie.Name.ToUpper() == "EMAIL")
                {
                    propertie.SetValue(t, email);
                }
                else
                {
                    var propertieType = propertie.PropertyType;
                    if (propertieType.FullName.ToUpper() == "System.Int64".ToUpper())
                    {
                        var value = claims.GetClaimsValue(propertie.Name);
                        propertie.SetValue(t, value.ToInt64());
                    }
                    if (propertieType.FullName.ToUpper() == "System.Int32".ToUpper())
                    {
                        var value = claims.GetClaimsValue(propertie.Name);
                        propertie.SetValue(t, value.ToInt32());
                    }
                    if (propertieType.FullName.ToUpper() == "System.Decimal".ToUpper())
                    {
                        var value = claims.GetClaimsValue(propertie.Name);
                        propertie.SetValue(t, value.ToDecimal(0));
                    }
                    if (propertieType.FullName.ToUpper() == "System.Double".ToUpper())
                    {
                        var value = claims.GetClaimsValue(propertie.Name);
                        propertie.SetValue(t, value.ToDouble(0));
                    }
                    if (propertieType.FullName.ToUpper() == "System.Single".ToUpper())
                    {
                        var value = claims.GetClaimsValue(propertie.Name);
                        propertie.SetValue(t, Convert.ToSingle(value));
                    }
                    if (propertieType.FullName.ToUpper() == "System.Boolean".ToUpper())
                    {
                        var value = claims.GetClaimsValue(propertie.Name);
                        propertie.SetValue(t, value.ToBoolean());
                    }
                    if (propertieType.FullName.ToUpper() == "System.String".ToUpper())
                    {
                        var value = claims.GetClaimsValue(propertie.Name);
                        propertie.SetValue(t, value.ToString());
                    }
                    if (propertieType.FullName.ToUpper() == "System.DateTime".ToUpper())
                    {
                        var value = claims.GetClaimsValue(propertie.Name);
                        propertie.SetValue(t, value.ToDateTime());
                    }
                }
            }

            return t;
        }
        /// <summary>
        /// 获取用户 Token
        /// </summary>
        /// <returns></returns>
        public static T GetUserToken<T>(this ClaimsPrincipal cp,T t) where T : class
        {
            var claims = cp.Claims;

            var uid = claims.GetClaimsValue(ClaimTypes.Name);
            var email = claims.GetClaimsValue(ClaimTypes.NameIdentifier);

            //获取类型
            Type type = typeof(T);
            // 获取所有属性集合
            var properties = type.GetProperties();

            foreach (var propertie in properties)
            {
                if (propertie.Name.ToUpper() == "UID")
                {
                    propertie.SetValue(t, string.IsNullOrEmpty(uid) ? string.Empty : uid);
                }
                else if (propertie.Name.ToUpper() == "EMAIL")
                {
                    propertie.SetValue(t, email);
                }
                else
                {
                    var propertieType = propertie.PropertyType;
                    if (propertieType.FullName.ToUpper() == "System.Int64".ToUpper())
                    {
                        var value = claims.GetClaimsValue(propertie.Name);
                        propertie.SetValue(t, value.ToInt64());
                    }
                    if (propertieType.FullName.ToUpper() == "System.Int32".ToUpper())
                    {
                        var value = claims.GetClaimsValue(propertie.Name);
                        propertie.SetValue(t, value.ToInt32());
                    }
                    if (propertieType.FullName.ToUpper() == "System.Decimal".ToUpper())
                    {
                        var value = claims.GetClaimsValue(propertie.Name);
                        propertie.SetValue(t, value.ToDecimal(0));
                    }
                    if (propertieType.FullName.ToUpper() == "System.Double".ToUpper())
                    {
                        var value = claims.GetClaimsValue(propertie.Name);
                        propertie.SetValue(t, value.ToDouble(0));
                    }
                    if (propertieType.FullName.ToUpper() == "System.Single".ToUpper())
                    {
                        var value = claims.GetClaimsValue(propertie.Name);
                        propertie.SetValue(t, Convert.ToSingle(value));
                    }
                    if (propertieType.FullName.ToUpper() == "System.Boolean".ToUpper())
                    {
                        var value = claims.GetClaimsValue(propertie.Name);
                        propertie.SetValue(t, value.ToBoolean());
                    }
                    if (propertieType.FullName.ToUpper() == "System.String".ToUpper())
                    {
                        var value = claims.GetClaimsValue(propertie.Name);
                        propertie.SetValue(t, value.ToString());
                    }
                    if (propertieType.FullName.ToUpper() == "System.DateTime".ToUpper())
                    {
                        var value = claims.GetClaimsValue(propertie.Name);
                        propertie.SetValue(t, value.ToDateTime());
                    }
                }
            }

            return t;
        }

        /// <summary>
        /// 获取设备串号
        /// </summary>
        /// <param name="controllerBase"></param>
        /// <returns></returns>
        public static string GetIMEI(this ControllerBase controllerBase)
        {
            var key = "IMEI";
            var header = controllerBase.HttpContext.Request.Headers[key];
            if (header.Count == 0)
            {
                header = controllerBase.HttpContext.Request.Headers["imei"];
                if (header.Count == 0) return string.Empty;
            }
            return header.ToString();
        }

    }

}
