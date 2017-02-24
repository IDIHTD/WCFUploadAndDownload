using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Activation;
using System.Text;
using System.Transactions;

namespace AutomaticUpdatesWCF
{
    // 注意: 使用“重构”菜单上的“重命名”命令，可以同时更改代码、svc 和配置文件中的类名“Service1”。
    // 注意: 为了启动 WCF 测试客户端以测试此服务，请在解决方案资源管理器中选择 Service1.svc 或 Service1.svc.cs，然后开始调试。
    // 注意: 使用“重构”菜单上的“重命名”命令，可以同时更改代码、svc 和配置文件中的类名“Service1”。
    // 注意: 为了启动 WCF 测试客户端以测试此服务，请在解决方案资源管理器中选择 Service1.svc 或 Service1.svc.cs，然后开始调试。
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class AutomaticUpdateImplement : IAutomaticUpdateServer
    {
        private static string path;
        private static string appInfoXMLPath;

        public AutomaticUpdateImplement()
        {
            path = FileProcessingHelper.GetUpLoadFilePath();
            appInfoXMLPath = FileProcessingHelper.AppInfoXMLPath();
        }

        /// <summary>
        /// 获取应用程序名称
        /// </summary>
        /// <param name="applicationName"></param>
        /// <returns></returns>
        public string GetVersionByApplicationName(string applicationName)
        {
            if (AppList == null || !AppList.Any())
            {
                var appXMLPath = appInfoXMLPath + applicationName + ".txt";
                if (File.Exists(appXMLPath))
                {
                    var getTxtString = File.ReadAllText(appXMLPath, Encoding.Default);
                    var appEntity= FileProcessingHelper.GetTFromXML<ApplicationEntity>(getTxtString);
                    if(appEntity!=null)
                    {
                        AppList = new List<ApplicationEntity>();
                        AppList.Add(appEntity);
                        return appEntity.AppVersion;
                    }
                }
                return string.Empty;
            }
            if (AppList.Exists(o => o.AppName == applicationName) && !string.IsNullOrEmpty(AppList.FirstOrDefault(o => o.AppName == applicationName).AppVersion))
                return AppList.FirstOrDefault(o => o.AppName == applicationName).AppVersion;
            return string.Empty;
        }

        /// <summary>
        /// 获取应用更新列表
        /// </summary>
        /// <param name="applicationName">应用名称</param>
        /// <returns></returns>
        public ApplicationEntity GetServerPublishFiles(string applicationName)
        {
            if (AppList != null && AppList.Any() && AppList.Any(o => o.AppName == applicationName))
            {
                return AppList.FirstOrDefault(o => o.AppName == applicationName);
            }
            else if(File.Exists(appInfoXMLPath + applicationName + ".txt"))
            {              
                    var getTxtString = File.ReadAllText(appInfoXMLPath + applicationName + ".txt", Encoding.Default);
                    var appEntity = FileProcessingHelper.GetTFromXML<ApplicationEntity>(getTxtString);
                    if (appEntity != null)
                    {
                    AppList = new List<ApplicationEntity>();
                        AppList.Add(appEntity);
                        return appEntity;
                    }
                return new ApplicationEntity();
            }
            else
                return new ApplicationEntity();
        }

        /// <summary>
        /// 应用程序列表
        /// </summary>
        public static List<ApplicationEntity> AppList
        {
            get; set;
        }

        /// <summary>
        /// 设置应用程序名称
        /// </summary>
        /// <param name="appInfo"></param>
        /// <returns></returns>
        public bool SetApplicationInfo(ApplicationInfo appInfo)
        {
            var rootName = string.Empty;
            if (appInfo != null && !string.IsNullOrEmpty(appInfo.AppPath))
                rootName = appInfo.AppPath.Substring(appInfo.AppPath.LastIndexOf("\\"));
            var currentPath = FileProcessingHelper.GetUpLoadFilePath() + appInfo.AppName + rootName;
            appInfo.AppPath = currentPath;
            var currentAppEntity = FileProcessingHelper.GetFiles(appInfo.AppPath);
            if (currentAppEntity != null && currentAppEntity.MDir != null)
            {
                currentAppEntity.AppName = appInfo.AppName;
                currentAppEntity.AppVersion = appInfo.AppVersion;
                if (AppList == null)
                    AppList = new List<ApplicationEntity>();
                AppList.Add(currentAppEntity);
                return true;
            }
            return false;
        }
        //public bool SetApplicationInfo(ApplicationInfo appInfo)
        //{
        //    var currentAppEntity = FileProcessingHelper.GetFiles(appInfo.AppPath);
        //    writeTxt("apppath:"+appInfo.AppPath+"1111");
        //    if (currentAppEntity != null && currentAppEntity.MDir != null)
        //    {
        //        currentAppEntity.AppName = appInfo.AppName;
        //        currentAppEntity.AppVersion = appInfo.AppVersion;
        //        if (AppList == null)
        //            AppList = new List<ApplicationEntity>();
        //        AppList.Add(currentAppEntity);
        //        return true;
        //    }
        //    return false;
        //}

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="dlfile"></param>
        /// <returns></returns>
        public DlFileResult DownLoadFile(DlFile dlfile)
        {
            string path = FileProcessingHelper.GetUpLoadFilePath() + dlfile.ProjectName + "\\" + dlfile.FileName;
            if (!File.Exists(path))
            {
                var result = new DlFileResult();
                result.Size = 0;
                result.IsSuccess = false;
                result.Message = "";
                result.FileStream = new MemoryStream();
                return result;
            }
            DlFileResult file = new DlFileResult();
            file.FileStream = new MemoryStream();
            Stream ms = new MemoryStream();
            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            fs.CopyTo(ms);
            ms.Position = 0;
            file.Size = ms.Length;
            file.FileStream = ms;
            file.IsSuccess = true;
            fs.Flush();
            fs.Close();
            return file;
        }

        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public UpFileResult UpLoadFile(UpFile file)
        {
            byte[] buffer = new byte[file.Size];
            FileStream fs = new FileStream(path + file.ProjectName + file.FileName, FileMode.Create, FileAccess.Write);
            int count = 0;

            while ((count = file.FileStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                fs.Write(buffer, 0, count);
            }

            fs.Flush();
            fs.Close();
            return new UpFileResult(true, "");
        }
    

        /// <summary>
        /// 如果路径不存在则创建，存在返回true
        /// </summary>
        /// <param name="dirName"></param>
        /// <returns></returns>
        public bool DirIsExistOrCreate(string dirName, string projectName)
        {
            if (!Directory.Exists(path + projectName + "\\" + dirName))
            {
                try
                {
                    Directory.CreateDirectory(path + projectName + "\\" + dirName);
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 获取项目列表(如果内存中有则不更新)
        /// </summary>
        /// <returns></returns>
        public List<ApplicationEntity> GetAppList()
        {
            var appList = AppList;
            if (appList != null && appList.Any())
            {
                return appList;
            }
            return UpdateAppListCommon();
        }

        /// <summary>
        /// 对外接口，强制更新IIS内存中项目信息列表
        /// </summary>
        /// <returns></returns>
        public List<ApplicationEntity> GetAppListFromTxtForce()
        {
          return  UpdateAppListCommon();
        }

        /// <summary>
        /// 从appInfo下读取项目信息列表，更新到IIS内存中
        /// </summary>
        /// <returns></returns>
        private List<ApplicationEntity> UpdateAppListCommon()
        {
           var appList = new List<ApplicationEntity>();
           
            var fileList = FileProcessingHelper.GetAppListFromTxt();
            if (fileList != null && fileList.Any())
            {
                if (AppList == null)
                    AppList = new List<ApplicationEntity>();
                AppList = fileList;
                appList = fileList;
                return appList;
            }
            return AppList ?? (AppList = new List<ApplicationEntity>());
        } 


        /// <summary>
        /// 删除指定目录下的所有文件和文件夹
        /// </summary>
        /// <param name="projectName"></param>
        /// <returns></returns>
        public bool DeleteFile(string projectName)
        {
            return FileProcessingHelper.DeleteDir(path + projectName + "\\");
        }

        public List<DifferentFile> GetDifferentList()
        {
            return new List<DifferentFile>();
        }

        /// <summary>
        /// 更新应用程序信息到XML中
        /// </summary>
        /// <param name="appName"></param>
       public bool UpdateAppInfo(string appName)
       {
            var isTrue = false;
            var firstOrDefault = AppList.FirstOrDefault(o => o.AppName == appName);
            if(firstOrDefault!=null)
            {
                try
                {
                    FileProcessingHelper.XMLSerializer(firstOrDefault, AppDomain.CurrentDomain.BaseDirectory + "\\AppInfo\\" + appName + ".txt");
                    isTrue = true;
                }
                catch(Exception ex)
                {
                    writeTxt(ex.ToString());
                }
               
            }
            return isTrue;
       }

       private void writeTxt(string txt)
       {
           FileStream fs = new FileStream(@"E:\AppUpdaterService\log.txt", FileMode.Append);
           //获得字节数组
           byte[] data = System.Text.Encoding.Default.GetBytes(txt+"\r\n");
           //开始写入
           fs.Write(data, 0, data.Length);
           //清空缓冲区、关闭流
           fs.Flush();
           fs.Close();
       }

        public bool DeleteAppInfo(string appName)
        {
            if (AppList != null && AppList.Any() && AppList.Any(o => o.AppName == appName))
            {
                using (var transactionScope=new TransactionScope())
                {
                    //删除内存中的数据
                   var effectCount=AppList.RemoveAll(o => o.AppName == appName);
                    //删除info下的txt
                    File.Delete(FileProcessingHelper.AppInfoXMLPath() + appName + ".txt");
                    //删除upload下的文件夹
                    FileProcessingHelper.DeleteDir(FileProcessingHelper.GetUpLoadFilePath() + appName);
                    if(effectCount>0)
                    transactionScope.Complete();
                }
            }
            return false;
        }
    }
}
