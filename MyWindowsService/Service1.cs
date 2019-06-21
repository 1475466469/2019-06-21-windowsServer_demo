using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data.SqlClient;
using Baidu;

namespace MyWindowsService
{
    public partial class Service1 : ServiceBase
    {
        string API_KEY = "N4fNwt5LzCNa1hX88nPI93hZ";
        string SECRET_KEY = "VKxr1FROhueYGesXk0pomr2YVRu0jyZG";
       private static FileStream F = new FileStream(@"D:\baiduLog.txt",
               FileMode.OpenOrCreate, FileAccess.ReadWrite);
       StreamWriter sw = new StreamWriter(F);
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {

            

            //开启定时任务

            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Enabled = true;
            timer.Interval = 1000;//执行间隔时间,单位为毫秒    
            timer.Start();
            timer.Elapsed += new System.Timers.ElapsedEventHandler(Timer1_Elapsed);

          

           
           
        }

        protected override void OnStop()
        {
        }
        private void Timer1_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {

            // 得到intHour,intMinute,intSecond，是当前系统时间    
            int intHour = e.SignalTime.Hour;
            int intMinute = e.SignalTime.Minute;
            int intSecond = e.SignalTime.Second;

            if (intHour == 9 && intMinute == 50 && intSecond == 10)
            {

                //获取当天更新的所有产品
                string dete = DateTime.Now.ToString("yyyy-MM-dd");

                string sqltext = "select  fGoodsCode,fsimplepicfile from  t_BOMM_GoodsMst where (fCDate>@Date or fModiDate>@Date ) and (fsimplepicfile is not null and fsimplepicfile<>'')";

                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@Date",dete)

                };
               

                //F.Close();

                DataTable dt = SqlHelper.SqlHelper.ExcuteDataTable(sqltext,parameters);
                foreach(DataRow item  in dt.Rows)
                {
                    //拼接上传路径
                    string path = @"D:\fgstamp" + item["fsimplepicfile"].ToString();
                    //拿到品号获取cont_sign将之前的图片从百度图库删除
                    string sql = "select * from BaiduUpload_info where fGoodsCode=@fGoodsCode";

                    SqlParameter[] parm = new SqlParameter[]
                {
                    new SqlParameter("@fGoodsCode",dete)

                };
                   DataTable DT= SqlHelper.SqlHelper.ExcuteDataTable(sql, parm);
                    try
                    {
                        string cont_sign = DT.Rows[0]["cont_sign"].ToString();
                        //删除百度图库，上传并更新数据库
                        handle(cont_sign, path, item["fGoodsCode"].ToString());
                        
                    }
                    catch(Exception EX)
                    {

                        sw.WriteLine("出现异常：" + EX.StackTrace);
                    }
                    










                }

                sw.Close();
                F.Close();



            };
            

        }


        private void handle(string contSign,string path,string fGoodsCode)
        {
            
            try
            {

                //删除百度
                var client = new Baidu.Aip.ImageSearch.ImageSearch(API_KEY, SECRET_KEY);
                var result = client.ProductDeleteBySign(contSign);
                //上传百度
               
                var image = File.ReadAllBytes(path);

                // 如果有可选参数
                var options = new Dictionary<string, object>{
                        {"brief", "{\"fGoodsCode\":\""+fGoodsCode+"\"}"},
                         {"url",path }
                };
                var res = client.ProductAdd(image, options);
                if (res.Count == 2)
                {

                   

                    //上传成功更新数据库

                    string sql2 = "update  BaiduUpload_info set cont_sign=@cont_sign where fGoodsCode=@fGoodsCode";
                    SqlParameter[] parameters2 = new SqlParameter[] {
                         new SqlParameter("@cont_sign",res["cont_sign"].ToString()),

                         new SqlParameter("@fGoodsCode",fGoodsCode)

                                  };
              int count= SqlHelper.SqlHelper.ExcuteNonQuery(sql2, parameters2);
                    if (count > 0)
                    {
                        sw.WriteLine(fGoodsCode + "更新成功"+ DateTime.Now.ToString("yyyy-MM-dd"));
                    }
                    else
                    {

                        string sql3 = "insert into  BaiduUpload_info values(@fGoodsCode,@cont_sign)";
                        SqlParameter[] p = new SqlParameter[] {
                         new SqlParameter("@cont_sign",res["cont_sign"].ToString()),
                         new SqlParameter("@fGoodsCode",fGoodsCode) };
                        int row = SqlHelper.SqlHelper.ExcuteNonQuery(sql3, p);
                        if (row > 0)
                        {
                            sw.WriteLine(fGoodsCode + "新增成功" + DateTime.Now.ToString("yyyy-MM-dd"));

                        }


                    }





                }








            }
                    catch (Exception EX)
                    {
                        Console.WriteLine(EX);
                        Console.WriteLine("--------删除失败----------");
                    }


               




                }
             





        




    }
}
