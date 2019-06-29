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
        
        private static FileStream F = new FileStream(@"D:\baidu_serviceLog.txt",
                FileMode.Append
            );
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

            sw.WriteLine("终止程序");
            sw.Close();
            F.Close();
        }
        private void Timer1_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {

       
            int intHour = e.SignalTime.Hour;
            int intMinute = e.SignalTime.Minute;
            int intSecond = e.SignalTime.Second;

            if (intHour ==22  && intMinute == 01 && intSecond == 10)
            {

                sw.WriteLine(DateTime.Now + "开始处理定时任务");
                //先处理历史工作
                HistoryWork();
                //获取当天更新的所有产品
                string dete = DateTime.Now.ToString("yyyy-MM-dd");
                string sqltext = "select  fGoodsCode,fsimplepicfile from  t_BOMM_GoodsMst where fDevProperty<>'2'and  (fCDate>@Date or fModiDate>@Date ) and (fsimplepicfile is not null and fsimplepicfile<>'')";
                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@Date",dete)

                };

                DataTable dt = SqlHelper.SqlHelper.ExcuteDataTable(sqltext, parameters);
                foreach (DataRow item in dt.Rows)
                {
                    //拼接上传路径
                    string path = @"D:\Dious_img\" + item["fsimplepicfile"].ToString();
                    //拿到品号获取cont_sign将之前的图片从百度图库删除
                    string sql = "select * from BaiduUpload_info where fGoodsCode=@fGoodsCode";

                    SqlParameter[] parm = new SqlParameter[]
                {
                    new SqlParameter("@fGoodsCode",item["fGoodsCode"].ToString())

                };
                    DataTable DT = SqlHelper.SqlHelper.ExcuteDataTable(sql, parm);
                    try
                    {
                        string cont_sign;
                        if (DT.Rows.Count > 0)
                        {
                         cont_sign = DT.Rows[0]["cont_sign"].ToString();
                            //删除百度图库，上传并更新数据库
                            handle(cont_sign, path, item["fGoodsCode"].ToString());
                        }
                        else
                        {

                            string sql_text = "insert into BaiduUpload_service values(@id,@fGoodsCode,@path,@workDate)";

                            SqlParameter[] parms = new SqlParameter[]
                            {
                            new SqlParameter("@id",Guid.NewGuid().ToString()),
                             new SqlParameter("@fGoodsCode",item["fGoodsCode"].ToString()),
                               new SqlParameter("@path", path),
                                new SqlParameter("@workDate",DateTime.Now)

                             };
                            int count = SqlHelper.SqlHelper.ExcuteNonQuery(sql_text, parms);

                            sw.WriteLine(item["fGoodsCode"].ToString() + "在原数据库中未找到！已记录row："+count);

                        }
                           

                    }
                    catch (Exception EX)
                    {

                        //文件找不到写入数据库

                        string sql_text = "insert into BaiduUpload_service values(@id,@fGoodsCode,@path,@workDate)";

                        SqlParameter[] parms = new SqlParameter[]
                        {
                            new SqlParameter("@id",Guid.NewGuid().ToString()),
                             new SqlParameter("@fGoodsCode",item["fGoodsCode"].ToString()),
                               new SqlParameter("@path", path),
                                new SqlParameter("@workDate",DateTime.Now)
                         };
                        int count = SqlHelper.SqlHelper.ExcuteNonQuery(sql_text, parms);

                        if (count > 0)
                        {
                            sw.WriteLine(item["fGoodsCode"].ToString() + "没有找到图片文件插入到数据库");
                        }
                        else
                        {
                            sw.WriteLine(item["fGoodsCode"].ToString() + "没有找到图片文件插入到数据库失败");
                        }

                        sw.WriteLine("出现异常：" + EX.Message);
                    }

                    System.Threading.Thread.Sleep(500);


                }

                sw.WriteLine("全部更新完成处理" + dt.Rows.Count + "条数据 处理结束时间" + DateTime.Now);

                sw.Close();
                F.Close();



            };


        }


        private void handle(string contSign, string path, string fGoodsCode)
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
                int count = SqlHelper.SqlHelper.ExcuteNonQuery(sql2, parameters2);
                if (count > 0)
                {
                    sw.WriteLine(fGoodsCode + "更新成功" + DateTime.Now.ToString("yyyy-MM-dd"));
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
            else
            {

                string error = res["error_code"].ToString();
                string id = Guid.NewGuid().ToString();
                string sql_text = "insert into BaiduUpload_err values(@id,@fGoodSCode,@flieName,@cont_same,@upLoadDate,@err_Code)";

                SqlParameter[] parameters = new SqlParameter[] {
                                 new SqlParameter("@id",id),
                                 new SqlParameter("@fGoodSCode",fGoodsCode),
                                new SqlParameter("@flieName",path),
                                 new SqlParameter("@cont_same",res["cont_sign"].ToString()),
                                 new SqlParameter("@upLoadDate",DateTime.Now),
                                 new SqlParameter("@err_Code",error),
                                      };
                int count = SqlHelper.SqlHelper.ExcuteNonQuery(sql_text, parameters);
                if (count > 0)
                {
                    sw.WriteLine(fGoodsCode + "更新失败" + DateTime.Now);
                }
                

            }
            

        }

        //处理历史没完成的任务
        public void HistoryWork()
        {

          
            //找出所有历史任务
            string sql = "select * from BaiduUpload_service where path<>''";

            var client = new Baidu.Aip.ImageSearch.ImageSearch(API_KEY, SECRET_KEY);
            DataTable dt = SqlHelper.SqlHelper.ExcuteDataTable(sql);
            foreach(DataRow item in dt.Rows)
            {
                try
                {
                    var image = File.ReadAllBytes(item["path"].ToString());
                    Dictionary<string, object> options = new Dictionary<string, object>{
                        {"brief", "{\"fGoodsCode\":\""+item["fGoodsCode"].ToString()+"\"}"},
                         {"url",item["path"].ToString() }
                };
                    var res = client.ProductAdd(image, options);
                    if (res.Count == 2)
                    {
                        //上传成功
                        string sql2 = "update  BaiduUpload_info set cont_sign=@cont_sign where fGoodsCode=@fGoodsCode";
                        SqlParameter[] parameters2 = new SqlParameter[] {
                         new SqlParameter("@cont_sign",res["cont_sign"].ToString()),
                         new SqlParameter("@fGoodsCode",item["fGoodsCode"].ToString())
                                  };
                        int count = SqlHelper.SqlHelper.ExcuteNonQuery(sql2, parameters2);
                        
                        if (count > 0)
                        {
                            sw.WriteLine("成功更新历史任务 品号：" + item["fGoodsCode"].ToString()+"--时间："+DateTime.Now);
                            string sql_deltete = "delete  from BaiduUpload_service where  fGoodsCode=@fGoodsCode";
                            SqlParameter[] parameters3 = new SqlParameter[] {

                                 new SqlParameter("@fGoodsCode",item["fGoodsCode"].ToString())
                            };
                            SqlHelper.SqlHelper.ExcuteNonQuery(sql_deltete, parameters3);

                        }
                        else
                        {
                            sw.WriteLine("更新历史任务失败 品号：" + item["fGoodsCode"].ToString() + "--时间：" + DateTime.Now);
                        }

                    }
                    else
                    {
                        //报错
                        string error = res["error_code"].ToString();
                        string id = Guid.NewGuid().ToString();
                        string sql_text = "insert into BaiduUpload_err values(@id,@fGoodSCode,@flieName,@cont_same,@upLoadDate,@err_Code)";

                        SqlParameter[] parameters = new SqlParameter[] {
                                 new SqlParameter("@id",id),
                                 new SqlParameter("@fGoodSCode",item["fGoodsCode"].ToString()),
                                new SqlParameter("@flieName",item["path"].ToString()),
                                 new SqlParameter("@cont_same",res["cont_sign"].ToString()),
                                 new SqlParameter("@upLoadDate",DateTime.Now),
                                 new SqlParameter("@err_Code",error),
                                      };
                        int count = SqlHelper.SqlHelper.ExcuteNonQuery(sql_text, parameters);

                        string sql_deltete = "delete  from BaiduUpload_service where  fGoodsCode=@fGoodsCode";

                        SqlParameter[] parameters3 = new SqlParameter[] {

                                 new SqlParameter("@fGoodsCode",item["fGoodsCode"].ToString())
                            };
                        SqlHelper.SqlHelper.ExcuteNonQuery(sql_deltete, parameters3);

                        Console.WriteLine("删除" + item["fGoodsCode"].ToString());




                        if (count > 0)
                        {
                            sw.WriteLine(item["fGoodsCode"].ToString() + "更新失败状态码" + error + "时间：" + DateTime.Now);
                        }
                        


                    }
                }
                catch (Exception ex)
                {

                    sw.WriteLine("处理历史任务出现异常" + ex.Message);
                }

                
                System.Threading.Thread.Sleep(1000);
                sw.Flush();
                F.Flush();

            }



            sw.WriteLine("处理历史" + dt.Rows.Count + "个"+DateTime.Now);





        }





    






        




    }
}
