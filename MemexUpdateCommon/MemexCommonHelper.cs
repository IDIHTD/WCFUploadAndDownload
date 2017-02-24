using MemexUpdateCommon.ServiceReference1;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Windows.Forms;
using System;

namespace MemexUpdateCommon
{
    public class MemexCommonHelper
    {
        private static string _projectName;
        private static IAutomaticUpdateServer channel;
        public event Action OnReplaceFile;

        public string AppPath { get; set; }
        public string AppName { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="projectName"></param>
        public MemexCommonHelper(string projectName, string path)
        {
            _projectName = projectName;
            AppName = projectName;
            AppPath = path;
            EndpointAddress address = new EndpointAddress("http://168.160.184.95:9113/AutomaticUpdateImplement.svc/IAutomaticUpdateServer");
            BasicHttpBinding binding = new BasicHttpBinding();
            binding.MaxBufferPoolSize = 2147483647;
            binding.MaxReceivedMessageSize = 2147483647;
            binding.MaxBufferSize = 2147483647;
            binding.Name = "BasicHttpBinding_IAutomaticUpdateServer";
            ChannelFactory<IAutomaticUpdateServer> factory = new ChannelFactory<IAutomaticUpdateServer>(binding, address);
            channel = factory.CreateChannel();
        }

        /// <summary>
        /// 删除服务端指定文件
        /// </summary>
        /// <param name="fileName"></param>
        public void DeleteFile(string fileName)
        {
            channel.DeleteFile(fileName);
        }

        /// <summary>
        /// 设置服务端项目属性
        /// </summary>
        /// <param name="appInfo"></param>
        /// <returns></returns>
        public bool SetApplicationInfo(ApplicationInfo appInfo)
        {
            return channel.SetApplicationInfo(appInfo);
        }

        /// <summary>
        /// 删除或创建指定文件夹
        /// </summary>
        /// <param name="dirName">文件夹路径</param>
        /// <param name="projectName">项目名称</param>
        /// <returns></returns>
        public bool DirIsExistOrCreate(string dirName)
        {
            return channel.DirIsExistOrCreate(dirName, _projectName);
        }

        /// <summary>
        /// 获取项目信息列表
        /// </summary>
        /// <returns></returns>
        private List<ApplicationEntity> GetAppList()
        {
            return channel.GetAppList();
        }


        /// <summary>
        /// 获取服务端文件列表实体
        /// </summary>
        /// <param name="projectName"></param>
        /// <returns></returns>
        public ApplicationEntity GetServerFiles()
        {
            return channel.GetServerPublishFiles(_projectName);
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
        public DlFileResult DownLoadFile(string fileName, out string msg, out long size, out Stream sm)
        {
            DlFile dfile = new DlFile();
            dfile.FileName = fileName;
            dfile.ProjectName = _projectName;
            msg = "";
            var result = channel.DownLoadFile(dfile);
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
        public UpFileResult UpLoadFile(string fileName, long length, Stream sm, out string message)
        {
            UpFile upFile = new UpFile { FileName = fileName, ProjectName = _projectName, Size = length, FileStream = sm };
            message = string.Empty;
            return channel.UpLoadFile(upFile);
        }

        /// <summary>
        /// 获取指定路径下的所有文件集合
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public ApplicationEntity GetFiles(string path)
        {
            return CommonAction.GetFiles(path);
        }

        /// <summary>
        /// 对比服务端和客户端文件
        /// </summary>
        /// <param name="server"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        public List<DifferentFile> GetDifferentFiles(MDirs server, MDirs client)
        {
            return CommonAction.GetDifferentFiles(server, client);
        }

        /// <summary>
        /// 删除客户端指定目录文件夹
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool DeleteDir(string path)
        {
            return CommonAction.DeleteDir(path);
        }

        /// <summary>
        /// 删除客户端指定目录的文件
        /// </summary>
        /// <param name="path"></param>
        public void DeleteFiles(string path)
        {
            CommonAction.DeleteFiles(path);
        }

        /// <summary>
        /// 是否需要更新
        /// </summary>
        /// <param name="projectName"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public bool IsNeedToUpdate(string version)
        {
            var appList = channel.GetAppList();
            if (appList != null && appList.Any())
            {
                var firstOrDefault = appList.FirstOrDefault(o => o.AppName == _projectName && o.AppVersion != version);
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
        public bool UpdateAppInfo()
        {
            return channel.UpdateAppInfo(_projectName);
        }

        public bool BeginUpdate()
        {
            if (string.IsNullOrEmpty(AppPath))
                MessageBox.Show("请先指定文件目录!");

            var clientApp = MemexUpateHelper.GetFiles(AppPath);
            var serverApp = MemexUpateHelper.GetServerFiles(AppName);

            clientApp.MDir.Files.RemoveAll(p => p.Name == "AppUpdater.exe" || p.Name == "MemexUpdateCommon.dll");
            serverApp.MDir.Files.RemoveAll(p => p.Name == "AppUpdater.exe" || p.Name == "MemexUpdateCommon.dll");

            if (serverApp != null && clientApp != null)
            {
                var listDifferent = MemexUpateHelper.GetDifferentFiles(serverApp.MDir, clientApp.MDir);
                if (listDifferent != null && listDifferent.ToList().Any())
                    StartDownFiles(listDifferent);
            }
            else
                MessageBox.Show("服务器端不存在要更新的文件！");
            return true;
        }

        #region 客户端下载文件方法

        public void StartDownFiles(List<DifferentFile> listDifferent)
        {
            if (listDifferent != null && listDifferent.Any())
            {
                //客户端需要下载的文件
                var desList = new List<DifDescription> { DifDescription.DirNotExistInClient, DifDescription.FileNotExistInClient, DifDescription.FileSizeInconsistency, DifDescription.FileVersionInconsistency, DifDescription.ForceUpdate, DifDescription.FileNotExistInServer, DifDescription.DirNotExistInServer };
                //需要下载的文件夹
                var needDownList = listDifferent.Where(o => desList.Contains(o.DiffentValue)).ToList();
                if (needDownList != null && needDownList.Any())
                {
                    var dirs = needDownList.Where(o => o.Type == FileType.Dir).ToList();
                    if (dirs != null && dirs.Any())
                        dirs.ForEach(o =>
                        {
                            //删除服务端不存在的文件夹
                            //if (o.DiffentValue == DifDescription.DirNotExistInServer)
                            //    MemexUpateHelper.DeleteDir(o.ClientFullPath);
                            if (o.DiffentValue == DifDescription.DirNotExistInClient)
                            {
                                o.FullName = o.FullName.Substring(2);
                                if (!Directory.Exists(o.ClientFullPath + "\\" + o.FilName))
                                    Directory.CreateDirectory(o.ClientFullPath + "\\" + o.FilName);
                            }
                        });
                }
                //需要下载的文件
                var needDownFiles = listDifferent.Where(o => o.Type == FileType.File).ToList();
                if (needDownFiles != null && needDownFiles.Any())
                    needDownFiles.ForEach(o =>
                    {
                        //删除服务端不存在的文件
                        //if (o.DiffentValue == DifDescription.FileNotExistInServer)
                        //    MemexUpateHelper.DeleteFiles(o.ClientFullPath);
                        if (o.DiffentValue == DifDescription.FileNotExistInClient || o.DiffentValue == DifDescription.FileSizeInconsistency || o.DiffentValue == DifDescription.FileVersionInconsistency)
                        {
                            //如果父目录不存在则创建
                            if (!string.IsNullOrEmpty(o.ClientFullPath) && !Directory.Exists(o.ClientFullPath.Substring(0, o.ClientFullPath.LastIndexOf("\\"))))
                                Directory.CreateDirectory(o.ClientFullPath.Substring(0, o.ClientFullPath.LastIndexOf("\\")));
                            //下载文件
                                DownFile(o);
                        }
                    });
            }
        }
        #endregion

        /// <summary>
        /// 调用WCF下载文件
        /// </summary>
        /// <param name="fileName"></param>
        private void DownFile(DifferentFile file)
        {
            Stream sm = new MemoryStream();
            var name = file.FullName.Substring(file.AbsoulteRootPath.Length - 1);
            var msg = string.Empty;
            var size = 0l;
            var issuccess = this.DownLoadFile(name, out msg, out size, out sm);
            if (issuccess.IsSuccess)
            {
                if (file.DiffentValue == DifDescription.FileNotExistInClient && !Directory.Exists(file.ClientFullPath))
                    Directory.CreateDirectory(file.ClientFullPath);
                var filePath = string.Empty;
                filePath = file.DiffentValue == DifDescription.FileNotExistInClient ? file.ClientFullPath + "\\" + file.FilName : file.ClientFullPath;

                byte[] buffer = new byte[size];

                FileStream fs;
                try
                {
                    fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);

                    int count = 0;
                    while ((count = sm.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fs.Write(buffer, 0, count);
                    }
                    fs.Flush();
                    fs.Close();
                }
                catch(Exception ex2)
                {
                    MessageBox.Show("写入文件时出错.file:"+filePath+"\n"+ex2.ToString());
                }
            }
        }
    }
}
