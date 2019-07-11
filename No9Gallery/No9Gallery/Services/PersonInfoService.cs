﻿using Microsoft.AspNetCore.Http;
using No9Gallery.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace No9Gallery.Services
{
    public class PersonInfoService:IPersonInfoService
    {
        //根据ID从数据库获取用户信息
        public  Task<PersonInfo> GetPersonInfoAsync(string UserID,string VisitID)
        {
            
            PersonInfo result= new PersonInfo();
            result.Workid = new List<string>();
            result.WorkPath = new List<string>();
            result.WorkNames = new List<string>();
            using (OracleConnection con = new OracleConnection(ConString.conString))
            {
                using (OracleCommand cmd = con.CreateCommand())
                {
                    try
                    {
                        con.Open();
                        cmd.BindByName = true;
                                            
                            cmd.CommandText = "select * from users where USER_ID = '" + UserID + "'";
                            OracleDataReader reader = cmd.ExecuteReader();

                            if (reader.Read() != false)
                            {
                                result.ID = reader.GetString(0);
                                result.VisitID = VisitID;
                                result.Name = reader.GetString(1);
                                result.Password = reader.GetString(2);
                                result.Status = reader.GetString(3);
                                result.HeadPortraitURL = reader.GetString(4);
                                reader.Dispose();
                            }                            
                            reader.Dispose();
                        if (result.Status == "Commom")
                        {

                            cmd.CommandText = "SELECT * FROM COMMON_USER WHERE USER_ID ='" + UserID + "'";
                            OracleDataReader myreader= cmd.ExecuteReader();
                            if(myreader.Read()!=false)
                            {
                                result.Introduction = myreader.GetString(1);
                                result.Integral = myreader.GetInt32(2).ToString();
                                result.membershipLevel = myreader.GetInt32(3).ToString();
                            }
                            cmd.CommandText = "select count(*) FROM follow WHERE FOLLOWER_ID = '" + VisitID + "' and FOLLOWED_ID = '" + UserID + "'";
                            result.IsFollowed = Convert.ToBoolean(cmd.ExecuteScalar());

                            //搜索该用户的所有作品id及路径
                            cmd.CommandText = "SELECT WORK_ID,HEADLINE,PICTURE  FROM WORK WHERE USER_ID ='" + UserID + "'";
                            OracleDataAdapter myadapter = new OracleDataAdapter(cmd);
                            DataTable dt = new DataTable();
                            myadapter.Fill(dt);
                            result.Worknum = dt.Rows.Count;
                            for (int i = 0; i < result.Worknum; i++)
                            {
                                result.Workid.Add(dt.Rows[i][0].ToString());
                                result.WorkNames.Add(dt.Rows[i][1].ToString());
                                result.WorkPath.Add(dt.Rows[i][2].ToString());

                            }
                        }
               
                       

                        con.Close();
                    }
                    catch (Exception ex)
                    {
                        string e = ex.Message;
                        int i = 0;
                    }
                    return Task.FromResult<PersonInfo>(result);
                }
            }
        }

        //确认修改信息
       public Task<string> ReviseInfo(ReviseInfo reviseinfo)
        {
            
            using (OracleConnection con = new OracleConnection(ConString.conString))
            {
                using (OracleCommand cmd = con.CreateCommand())
                {
                    try
                    {
                        con.Open();
                        cmd.BindByName = true;
                        //var commandText = "insert into PERSON (ID,PEOPLE_TROUERSCOLOR,CAR_NUMBER,CAMERA,PEOPLE_JACKETTYPE,IMG_ID) values(23,'hong', 'ss', 'ss' ,'ss','ss132' )";
                        cmd.CommandText = "UPDATE USERS SET USER_NAME = '"+reviseinfo.NewUserName+"' , PASSWORD = '"+reviseinfo.NewPassword+"' WHERE USER_ID = '"+reviseinfo.UserID+"'";
                        cmd.ExecuteNonQuery();
                        if (reviseinfo.NewIntroduction.Length > 0)
                        {
                            cmd.CommandText = "UPDATE COMMON_USER SET INTRODUCTION = '" + reviseinfo.NewIntroduction + "' WHERE USER_ID = '" + reviseinfo.UserID + "'";
                            cmd.ExecuteNonQuery();
                        }
                        con.Close();
                    }
                    catch (Exception ex)
                    {
                        string e = ex.Message;
                        int i = 0;
                        return Task.FromResult(e);
                    }
                    return Task.FromResult("Succed");
                }
               
            }
        }


        //将头像图片保存到服务器上并进行数据库的更新
        public async Task<string>  Uploadavatar(string id,List<IFormFile> files)
        {
            if (files.Count == 0)
                return "Doesn't received image";
            long size = files.Sum(f => f.Length);

            var avatarfolder = "..\\No9Gallery\\wwwroot\\image\\avatar";
            if (!Directory.Exists(avatarfolder))
                Directory.CreateDirectory(avatarfolder);

            if (files[0].Length > 0)
            {
                var fileName = DateTime.Now.ToString("yyyyMMddHHmmss")
                    + Path.GetExtension(files[0].FileName);
                
                var filePath = Path.Combine(avatarfolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await files[0].CopyToAsync(stream);
                }
                string oldavatarpath=null;
                using (OracleConnection con = new OracleConnection(ConString.conString))
                {
                    
                    using (OracleCommand cmd = con.CreateCommand())
                    {
                        try
                        {
                            con.Open();
                            cmd.BindByName = true;

                            //先找出旧头像，若不是系统提供的默认头像，就删除
                            cmd.CommandText = "SELECT AVATAR FROM USERS WHERE USER_ID = '" + id + "'";
                            OracleDataReader reader = cmd.ExecuteReader();
                            if (reader.Read() != false)
                            {
                                if(reader.GetString(0)!="Default.png")
                                {
                                    oldavatarpath = "..\\No9Gallery\\wwwroot\\image\\avatar\\" + reader.GetString(0);
                                    File.Delete(oldavatarpath);
                                }                               
                               reader.Dispose();
                            }
                            //更新头像路径
                            cmd.CommandText = "UPDATE USERS SET AVATAR = '"+fileName+"' WHERE USER_ID = '"+id+"'";
                            cmd.ExecuteNonQuery();
                           
                            con.Close();

                        }
                        catch (Exception ex)
                        {
                            string e = ex.Message;
                            int i = 0;
                            return e;
                        }

                        
                        
                    }

                }


                return "succed";
            }
            else
                return "Empty Image";
        }


        //获取积分记录
        public Task<List<Chargelist>> GetChargeListAsync(string id)
        {
            List<Chargelist> result = new List<Chargelist>();

            using (OracleConnection con = new OracleConnection(ConString.conString))
            {

                using (OracleCommand cmd = con.CreateCommand())
                {
                    try
                    {
                        con.Open();
                        cmd.BindByName = true;
                        cmd.CommandText = "SELECT * FROM POINT_RECORD WHERE USER_ID = '" + id + "'";
                        OracleDataAdapter myadapter = new OracleDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        myadapter.Fill(dt);
                        DataView dv = dt.DefaultView;//获取表视图

                        dv.Sort = "TIME DESC";//按照TIME倒序排序
                        dt=dv.ToTable();//转为表
                        var rowcount = dt.Rows.Count;
                        
                        for (int i=0;i<rowcount;i++)
                        {
                            Chargelist item = new Chargelist();
                            item.order_no = dt.Rows[i][0].ToString();
                            item.user_ID = dt.Rows[i][1].ToString();
                            item.amount = Convert.ToInt32(dt.Rows[i][2]);
                            item.content= dt.Rows[i][3].ToString();
                            item.time = Convert.ToDateTime(dt.Rows[i][4]);
                            result.Add(item);
                        }
                       
                        con.Close();

                    }
                    catch (Exception ex)
                    {
                        string e = ex.Message;
                        int i = 0;
                        return null;
                    }

                    return  Task.FromResult(result);

                }

            }
            
        }


        //根据id获取当前积分
        public Task<int> GetPointsAsync(string id)
        {
            int result = 0;
            using (OracleConnection con = new OracleConnection(ConString.conString))
            {

                using (OracleCommand cmd = con.CreateCommand())
                {
                    try
                    {
                        con.Open();
                        cmd.BindByName = true;
                        cmd.CommandText = "SELECT points FROM COMMON_USER WHERE USER_ID = '"+id+"'";

                        result = Convert.ToInt32(cmd.ExecuteScalar());                        
                        

                        con.Close();

                    }
                    catch (Exception ex)
                    {
                        string e = ex.Message;
                        int i = 0;
                        return null;
                    }

                    return Task.FromResult(result);

                }

            }

            return Task.FromResult(result);
        }

        //提交订单
       public Task<string> ChargeSubmit(string order_no, string id, int amount, string ConTent, string chargetime,int points)
        {

            int result = 0;
            using (OracleConnection con = new OracleConnection(ConString.conString))
            {

                using (OracleCommand cmd = con.CreateCommand())
                {
                    try
                    {
                        con.Open();
                        cmd.BindByName = true;
                        cmd.CommandText = "UPDATE COMMON_USER SET POINTS = '" + points + "' WHERE USER_ID = '" + id + "'";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT INTO POINT_RECORD VALUES ('" + order_no + "','" + id + "'," +amount+",'"+ConTent+"',to_date ( '"+chargetime+"','YYYY-MM-DD HH24:MI:SS' ))";
                        var text= cmd.CommandText;
                        cmd.ExecuteNonQuery();
                        //INSERT INTO  FLOOR VALUES(to_date ( '2007-12-20 18:31:34' , 'YYYY-MM-DD HH24:MI:SS' ) ) ;
                        con.Close();

                    }
                    catch (Exception ex)
                    {
                        string e = ex.Message;
                        int i = 0;
                        return null;
                    }

                    return Task.FromResult("Succed");

                }

            }            
        }


        //修改关注状态
        public Task<string> ChangeFollow(string visitid,string id)
        {
            int result = 0;
            using (OracleConnection con = new OracleConnection(ConString.conString))
            {

                using (OracleCommand cmd = con.CreateCommand())
                {
                    try
                    {
                        con.Open();
                        cmd.BindByName = true;
                        cmd.CommandText = "select count(*) FROM follow WHERE FOLLOWER_ID = '" + visitid + "' and FOLLOWED_ID = '" + id + "'";
                        bool follows = Convert.ToBoolean(cmd.ExecuteScalar());
                        //cmd.CommandText = "UPDATE COMMON_USER SET POINTS = '" + points + "' WHERE USER_ID = '" + id + "'";
                        if (follows)
                        {
                            cmd.CommandText = "DELETE FROM FOLLOW WHERE FOLLOWER_ID = '" + visitid + "' and FOLLOWED_ID = '" + id + "'";
                        }
                        else
                        {
                            cmd.CommandText = "INSERT INTO FOLLOW VALUES ('" + visitid + "','" + id + "')";
                        }
                        //INSERT INTO  FLOOR VALUES(to_date ( '2007-12-20 18:31:34' , 'YYYY-MM-DD HH24:MI:SS' ) ) ;

                        cmd.ExecuteNonQuery();
                        con.Close();

                    }
                    catch (Exception ex)
                    {
                        string e = ex.Message;
                        int i = 0;
                        return null;
                    }

                    return Task.FromResult("Succed");

                }

            }

            return Task.FromResult("Succed");
        }


        //上传作品
        public async Task<string> UploadWork(string Userid,IFormFile file, string Workname,string  WorkType,string  Introduction)
        {

           
            var Worksfolder = "..\\No9Gallery\\wwwroot\\image\\works\\";
            if (!Directory.Exists(Worksfolder))
                Directory.CreateDirectory(Worksfolder);
            //随机产生作品ID
            var randomnum = new Random();           
            var fileid = DateTime.Now.ToString("yyyyMMddHHmmss")+randomnum.Next(0,1000).ToString();
            //生成上传时间
            var uploadtime= DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            //生成下载所需积分
            int poitsneed = 200;
            //生成文件路径
            var filefolder = "/image/works/";
            string workpath =fileid+ Path.GetExtension(file.FileName);

           
            using (var stream = new FileStream(Worksfolder+fileid+ Path.GetExtension(file.FileName), FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }


            using (OracleConnection con = new OracleConnection(ConString.conString))
            {

                using (OracleCommand cmd = con.CreateCommand())
                {
                    try
                    {
                        con.Open();
                        cmd.BindByName = true;
                        cmd.CommandText = "INSERT INTO WORK VALUES('" + fileid + "','" + Userid + "','" + Workname + "','" + Introduction + "','" + workpath + "'," + 0 + "," + 0 + ",to_date ('" + uploadtime + "','YYYY-MM-DD HH24:MI:SS')," + poitsneed + ")";
                        // cmd.CommandText = "INSERT INTO POINT_RECORD VALUES ('" + order_no + "','" + id + "'," + amount + ",'" + ConTent + "',to_date ( '" + chargetime + "','YYYY-MM-DD HH24:MI:SS' ))";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT INTO WORK_TYPE VALUES('" + WorkType + "','" + fileid + "')";
                        cmd.ExecuteNonQuery();
                        con.Close();

                    }
                    catch (Exception ex)
                    {
                        string e = ex.Message;
                        int i = 0;
                        return null;
                    }

                    return "Succed";

                }

            }
          
        }



        //执行升级操作
        public Task<string> Upgrade(string id,string Level,string Points)
        {
            using (OracleConnection con = new OracleConnection(ConString.conString))
            {

                using (OracleCommand cmd = con.CreateCommand())
                {
                    try
                    {
                        con.Open();
                        cmd.BindByName = true;
                        cmd.CommandText = "UPDATE COMMON_USER SET MEMBERSHIP_LEVEL = "+(Convert.ToInt32(Level)+1)+",POINTS = "+(Convert.ToInt32(Points)-5000)+"WHERE USER_ID = '"+id+"'";
                      
                        cmd.ExecuteNonQuery();
                        con.Close();
                    }
                    catch (Exception ex)
                    {
                        string e = ex.Message;
                        int i = 0;
                        return Task.FromResult("false");
                    }                   

                }

            }


            return Task.FromResult("true");
        }



        //获取系统中当前存在的所有报告
        public Task<List<MessageList>> GetReportAsync()
        {
            List<MessageList> Reports = new List<MessageList>();
            using (OracleConnection con = new OracleConnection(ConString.conString))
            {

                using (OracleCommand cmd = con.CreateCommand())
                {
                    try
                    {
                        con.Open();
                        cmd.BindByName = true;
                        cmd.CommandText = "SELECT * FROM REPORT";
                        OracleDataAdapter myadapter = new OracleDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        myadapter.Fill(dt);
                        DataView dv = dt.DefaultView;//获取表视图

                        dv.Sort = "STATE DESC";//按照STATE倒序排序
                        dt = dv.ToTable();//转为表
                        var rowcount = dt.Rows.Count;
                        con.Close();

                        for(int i=0;i<rowcount;i++)
                        {
                            MessageList Item = new MessageList();
                            Item.works_ID= dt.Rows[i][0].ToString();
                            Item.user_ID = dt.Rows[i][1].ToString();
                            Item.report_time =Convert.ToDateTime(dt.Rows[i][2]);
                            Item.report_content = dt.Rows[i][3].ToString();
                            if(Convert.ToBoolean(Convert.ToInt32(dt.Rows[i][4])))
                            {
                                Item.state = "已读";
                            }
                            else
                            {
                                Item.state = "未读";
                            }
                            Reports.Add(Item);
                        }
                    }
                    catch (Exception ex)
                    {
                        string e = ex.Message;
                        int i = 0;
                        return Task.FromResult(Reports);
                    }
                    return Task.FromResult(Reports);

                }

            }
        }



        //管理员发送信息
       public Task<string> PostMassage(string adminID,string ReceiverID, string Content,string Time)
        {

            string MassageID = DateTime.Now.ToString("yyyyMMddHHmmss");
            using (OracleConnection con = new OracleConnection(ConString.conString))
            {

                using (OracleCommand cmd = con.CreateCommand())
                {
                    try
                    {
                        con.Open();
                        cmd.BindByName = true;
                        cmd.CommandText = "INSERT INTO MESSAGE VALUES('"+MassageID+"','"+adminID+"','"+Content+"',to_date('"+Time+"','YYYY-MM-DD HH24:MI:SS'))";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT INTO RECEIVE VALUES('" + ReceiverID + "','" + MassageID + "'," + 0 + ")";
                        cmd.ExecuteNonQuery();
                        //cmd.CommandText = "INSERT INTO POINT_RECORD VALUES ('" + order_no + "','" + id + "'," + amount + ",'" + ConTent + "',to_date ( '" + chargetime + "','YYYY-MM-DD HH24:MI:SS' ))";

                    }
                    catch (Exception ex)
                    {
                        string e = ex.Message;
                        int i = 0;
                        return Task.FromResult("Failed");
                    }
                    return Task.FromResult("Succeed");

                }

            }
           
        }

        //更新信息状态
        public Task<string> ChangeMessage()
        {

            using (OracleConnection con = new OracleConnection(ConString.conString))
            {

                using (OracleCommand cmd = con.CreateCommand())
                {
                    try
                    {
                        con.Open();
                        cmd.BindByName = true;
                        cmd.CommandText = "UPDATE REPORT SET STATE ="+1;
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        string e = ex.Message;
                        int i = 0;
                        return Task.FromResult("Failed");
                    }
                    return Task.FromResult("Succeed");

                }

            }
        }


        //获取当前用户收到的信息
        public Task<List<MessageReceivelist>> GetMessageAsync(string id)
        {
            List<MessageReceivelist> Messages = new List<MessageReceivelist>();
            using (OracleConnection con = new OracleConnection(ConString.conString))
            {

                using (OracleCommand cmd = con.CreateCommand())
                {
                    try
                    {
                        con.Open();
                        cmd.BindByName = true;
                        cmd.CommandText = "select MESSAGE.MESSAGE_ID,ADMINISTRATOR_ID, IS_READ,TIME,CONTENT FROM RECEIVE,MESSAGE WHERE RECEIVE.MESSAGE_ID=MESSAGE.MESSAGE_ID AND RECEIVE.USER_ID = '"+id+"'";
                        OracleDataAdapter myadapter = new OracleDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        myadapter.Fill(dt);
                        DataView dv = dt.DefaultView;//获取表视图

                        dv.Sort = "TIME DESC";//按照STATE倒序排序
                        dt = dv.ToTable();//转为表
                        var rowcount = dt.Rows.Count;
                      
                        for(int i=0;i<rowcount;i++)
                        {
                            MessageReceivelist Item = new MessageReceivelist();
                            Item.message_ID = dt.Rows[i][0].ToString();
                            Item.user_ID = dt.Rows[i][1].ToString();
                            if(Convert.ToBoolean(Convert.ToInt32(dt.Rows[i][2])))
                            {
                                Item.is_read = "已读";
                            }
                            else
                            {
                                Item.is_read = "未读";
                            }
                            Item.date= Convert.ToDateTime(dt.Rows[i][3]);
                            Item.content = dt.Rows[i][4].ToString();

                            Messages.Add(Item);

                        }
                    }
                    catch (Exception ex)
                    {
                        string e = ex.Message;
                        int i = 0;
                        return Task.FromResult(Messages);
                    }
                    return Task.FromResult(Messages);

                }

            }
        }


        public Task<string> setRead(string msgid)
        {
            using (OracleConnection con = new OracleConnection(ConString.conString))
            {

                using (OracleCommand cmd = con.CreateCommand())
                {
                    try
                    {
                        con.Open();
                        cmd.BindByName = true;
                        cmd.CommandText = "UPDATE RECEIVE SET IS_READ =" + 1+"WHERE MESSAGE_ID = '"+msgid+"'";
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        string e = ex.Message;
                        int i = 0;
                        return Task.FromResult("Failed");
                    }
                    return Task.FromResult("Succeed");

                }

            }
        }
    }
}
