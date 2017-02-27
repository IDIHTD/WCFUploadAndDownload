using MemexUpdateCommon.ServiceReference1;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System;
using LogCommon;

namespace MemexUpdateCommon
{
   public class MemexUpateHelper
    {       
        private static IAutomaticUpdateServer channel;
        public event Action onFileUpload;

        static MemexUpateHelper()
        {
             EndpointAddress address = new EndpointAddress("http://168.160.184.95:9113/AutomaticUpdateImplement.svc/IAutomaticUpdateServer");
            //EndpointAddress address = new EndpointAddress("http://10.20.20.40:60124/AutomaticUpdateImplement.svc/IAutomaticUpdateServer");
            BasicHttpBinding binding = new BasicHttpBinding();
            binding.MaxBufferPoolSize = 2147483647;
            binding.MaxReceivedMessageSize = 2147483647;
            binding.MaxBufferSize = 2147483647;
            binding.Name = "BasicHttpBinding_IAutomaticUpdateServer";
            ChannelFactory<IAutomaticUpdateServer> factory = new ChannelFactory<IAutomaticUpdateServer>(binding,address);
            channel = factory.CreateChannel();
        }
        
        /// <summary>
        /// 删除服务端指定文件
        /// </summary>
        /// <param name="fileName"></param>
        public static void DeleteFile(string fileName)
        {
            channel.DeleteFile(fileName);
        }

        /// <summary>
        /// 设置服务端项目属性
        /// </summary>
        /// <param name="appInfo"></param>
        /// <returns></returns>
        public static bool SetApplicationInfo(ApplicationInfo appInfo)
        {
          return channel.SetApplicationInfo(appInfo);
        }

        /// <summary>
        /// 删除或创建指定文件夹
        /// </summary>
        /// <param name="dirName">文件夹路径</param>
        /// <param name="projectName">项目名称</param>
        /// <returns></returns>
        public static bool DirIsExistOrCreate(string dirName,string projectName)
        {
            return channel.DirIsExistOrCreate(dirName, projectName);
        }

        /// <summary>
        /// 获取项目信息列表
        /// </summary>
        /// <returns></returns>
        public static List<ApplicationEntity> GetAppList()
        {
            return channel.GetAppList();
        }


       public static List<ApplicationEntity> GetAppListForce()
       {
           return channel.GetAppListFromTxtForce();
       } 

        /// <summary>
        /// 获取服务端文件列表实体
        /// </summary>
        /// <param name="projectName"></param>
        /// <returns></returns>
        public static ApplicationEntity GetServerFiles(string projectName)
        {
           return channel.GetServerPublishFiles(projectName);
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="fileName">文件名称</param>
        /// <param name="projectName">项目名称</param>
        /// <param name="msg">返回消息</param>
        /// <param name="size">大小</param>
        /// <param name="sm">文件流</param>
        /// <returns></returns>
        public static DlFileResult DownLoadFile(string fileName, string projectName, out string msg, out long size, out Stream sm)
        {
            DlFile dfile = new DlFile();
            dfile.FileName = fileName;
            dfile.ProjectName = projectName;
            msg = "";
            var result= channel.DownLoadFile(dfile);
            size = result.Size;
            sm = result.FileStream;
            return result;
        }

        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="fileName">文件名称</param>
        /// <param name="projectName">项目名称</param>
        /// <param name="length">长度</param>
        /// <param name="sm">文件流</param>
        /// <param name="message">消息</param>
        /// <returns></returns>     
        public static UpFileResult UpLoadFile(string fileName, string projectName, long length, Stream sm, out string message)
        {
            UpFile upFile = new UpFile { FileName = fileName, ProjectName = projectName, Size = length, FileStream = sm };
            message = string.Empty;
            return channel.UpLoadFile(upFile);
        }

        /// <summary>
        /// 获取指定路径下的所有文件集合
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static ApplicationEntity GetFiles(string path)
        {
            MyLog4NetInfo.LogInfo("文件日志："+path);
            return CommonAction.GetFiles(path);
        }

        /// <summary>
        /// 对比服务端和客户端文件
        /// </summary>
        /// <param name="server"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        public static List<DifferentFile> GetDifferentFiles(MDirs server, MDirs client)
        {
            return CommonAction.GetDifferentFiles(server,client);
        }

        /// <summary>
        /// 删除客户端指定目录文件夹
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool DeleteDir(string path)
        {
            return CommonAction.DeleteDir(path);
        }

        /// <summary>
        /// 删除客户端指定目录的文件
        /// </summary>
        /// <param name="path"></param>
        public static void DeleteFiles(string path)
        {
            CommonAction.DeleteFiles(path);
        }

        /// <summary>
        /// 是否需要更新
        /// </summary>
        /// <param name="projectName"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public static bool IsNeedToUpdate(string projectName,string version)
        {
           var appList= channel.GetAppList();
            if (appList != null && appList.Any())
            {
                var firstOrDefault = appList.FirstOrDefault(o => o.AppName == projectName && o.AppVersion != version);
                if (firstOrDefault != null)
                    return true;
                return false;
            }
            return false;
        }

        /// <summary>
        /// 更新项目文件信息到XML中
        /// </summary>
        /// <param name="appName"></param>
        /// <returns></returns>
        public static bool UpdateAppInfo(string appName)
        {
            return channel.UpdateAppInfo(appName);
        }

        /// <summary>
        /// 删除指定名称的App信息
        /// </summary>
        /// <param name="appName"></param>
        /// <returns></returns>
        public static bool DeleteAppInfo(string appName)
        {
            return channel.DeleteAppInfo(appName);
        }

       /// <summary>
       /// 将日志写入到服务端
       /// </summary>
       /// <param name="message">日志内容</param>
       /// <param name="isLog">true为日志,false为错误日志</param>
       public static void WriteLog(string message,bool isLog)
       {
           channel.WriteLog(string.Format("调用计算机IP为:{0}",GetLocalIP())+message, isLog);
       }

        /// <summary>
        /// 获取本机IP
        /// </summary>
        /// <returns></returns>
       public static string GetLocalIP()
       {
           return CommonAction.GetLocalIP();
       }
    }
}
