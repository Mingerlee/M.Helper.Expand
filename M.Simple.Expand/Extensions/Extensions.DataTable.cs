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
            return dt == null || dt.Columns.Count == 0 || dt.Rows.Count <= 0;
        }
        /// <summary>
        /// <para>M.Simple.Expand扩展</para>
        /// <para>根据条件筛选DataTable的行（类似Linq的Where）</para>
        /// </summary>
        /// <param name="dt">源DataTable</param>
        /// <param name="predicate">筛选条件</param>
        /// <returns>筛选后的新DataTable</returns>
        public static DataTable Where(this DataTable dt, Func<DataRow, bool> predicate)
        {
            // 校验源表是否为空
            if (dt == null || dt.Rows.Count == 0)
                return dt.Clone(); // 返回结构相同但无数据的表

            // 筛选符合条件的行并创建新表
            var filteredRows = dt.Rows.Cast<DataRow>().Where(predicate).ToArray();
            var newDt = dt.Clone();
            foreach (var row in filteredRows)
            {
                newDt.ImportRow(row);
            }
            return newDt;
        }
    }
}
