using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Threading.Tasks;

namespace  SqlHelper
{
    public class SqlHelper
    {
        #region 返回连接字符串
        /// <summary>
        /// 返回连接字符串
        /// </summary>
        /// <returns>连接字符串</returns>
        public static string GetSqlConnectionString()
        {

            string sql = "Data Source=192.168.10.9;Initial Catalog=DCF19;User ID=sa;Password=!888dious999;Max Pool Size=200";

            return sql;

        } 
        #endregion

        #region 封装一个执行sql 返回受影响的行数
        /// <summary>
        /// 执行一个sql，返回影响行数
        /// </summary>
        /// <param name="sqlText">执行的sql脚本</param>
        /// <param name="parameters">参数集合</param>
        /// <returns>受影响的行数</returns>
        public static int ExcuteNonQuery(string sqlText,params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(GetSqlConnectionString()))
            {
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = sqlText;
                    cmd.Parameters.AddRange(parameters);//把参数添加到cmd命令中。
                    return cmd.ExecuteNonQuery();
                }
            }
        }
        #endregion

        #region 执行sql。返回 查询结果中的 第一行第一列的值

        public static object ExcuteScalar(string sqlText, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(GetSqlConnectionString()))
            {
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = sqlText;
                    cmd.Parameters.AddRange(parameters);
                    return cmd.ExecuteScalar();
                }
            }
        }

        public static T ExcuteScalar<T>(string sqlText, params SqlParameter[] parameters)
            //where T:UserInfo要求必须继承某个类型
            //where T:class //必须是类
            //where T:new () //要求T必须有默认构造函数
            
            //where 可以现在T类型，必须是class，必须有构造函数，必须继承或者实现某个类或者接口。
        {
            using (SqlConnection conn = new SqlConnection(GetSqlConnectionString()))
            {
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = sqlText;
                    cmd.Parameters.AddRange(parameters);
                    return (T)cmd.ExecuteScalar();
                    //int? i = 0;
                    //object num = i;
                    //i = num as int?;
                }
            }
        }
        #endregion

        #region 执行sql 返回一个DataTable

        public static DataTable ExcuteDataTable(string sqlText, params SqlParameter[] parameters)
        {
            #region MyRegion
            //using (SqlDataAdapter adapter = new SqlDataAdapter(sqlText, GetSqlConnectionString()))
            //{
            //    DataTable dt = new DataTable();
            //    adapter.SelectCommand.Parameters.AddRange(parameters);
            //    adapter.Fill(dt);
            //    return dt;
            //} 
            #endregion

            return ExcuteDataTable(sqlText, CommandType.Text, parameters);
        }
        #endregion

        #region 执行sql 脚本，返回一个SqlDataReader

        public static SqlDataReader ExucteReader(string sqlText, params SqlParameter[] parameters)
        {
            //SqlDataReader要求，它读取数据的时候，它哟啊独占 它的SqlConnection对象，而且SqlConnection必须是Open状态。
            SqlConnection conn =new SqlConnection(GetSqlConnectionString());//不要释放连接，因为后面还要需要连接打开状态。
            SqlCommand cmd = conn.CreateCommand();
            conn.Open();
            cmd.CommandText = sqlText;
            cmd.Parameters.AddRange(parameters);
            //CommandBehavior.CloseConnection:代表，当SqlDataReader释放的时候，顺便把SqlConnection对象也释放掉。
            return cmd.ExecuteReader(CommandBehavior.CloseConnection);
        }
        #endregion

        #region 提供一个  可以 执行存储过程的方法。
          public static DataTable ExcuteDataTable(string sqlText,CommandType commandType, params SqlParameter[] parameters)
        {
            using (SqlDataAdapter adapter = new SqlDataAdapter(sqlText, GetSqlConnectionString()))
            {
                DataTable dt =new DataTable();
                adapter.SelectCommand.Parameters.AddRange(parameters);
                //兼容存储过程
                adapter.SelectCommand.CommandType = commandType;
                adapter.Fill(dt);
                return dt;
            }
        }
        #endregion
    }
}
