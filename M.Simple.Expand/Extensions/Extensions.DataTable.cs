using System.Data;
using System.Reflection;

namespace M.Simple.Expand
{
    public static partial class Extensions
    {
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>将DataTable数据转换成List</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static List<T> ToLists<T>(this DataTable dt) where T : class, new()
        {
            //创建一个属性的列表
            List<PropertyInfo> prlist = [];
            //获取TResult的类型实例  反射的入口
            Type t = typeof(T);
            //获得TResult 的所有的Public 属性 并找出TResult属性和DataTable的列名称相同的属性(PropertyInfo) 并加入到属性列表
            Array.ForEach(t.GetProperties(), p => { if (dt.Columns.IndexOf(p.Name) != -1) prlist.Add(p); });
            //创建返回的集合
            List<T> oblist = [];

            foreach (DataRow row in dt.Rows)
            {
                //创建TResult的实例
                T ob = new();
                //找到对应的数据  并赋值
                prlist.ForEach(p => { if (row[p.Name] != DBNull.Value) p.SetValue(ob, row[p.Name], null); });
                //放入到返回的集合中.
                oblist.Add(ob);
            }
            return oblist;
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>    
        /// <para>是否为NULL或者为空</para>    
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty(this DataTable dt)
        {
            return dt == null || dt.Rows.Count <= 0;
        }
    }
}
