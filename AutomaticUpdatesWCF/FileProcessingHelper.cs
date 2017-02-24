using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using LogCommon;


namespace AutomaticUpdatesWCF
{
    public static class FileProcessingHelper
    {
        public static string GetUpLoadFilePath()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + @"UpLoadFile\";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        public static string AppInfoXMLPath()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\AppInfo\\";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>
        /// 删除指定目录的文件夹及文件夹下的文件
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool DeleteDir(string path)
        {
            if (Directory.Exists(path) == false)
            {
                return false;
            }
            DirectoryInfo dir = new DirectoryInfo(path);
            FileInfo[] files = dir.GetFiles();
            try
            {
                foreach (var item in files)
                {
                    File.Delete(item.FullName);
                }
                if (dir.GetDirectories().Length != 0)
                {
                    foreach (var item in dir.GetDirectories())
                    {
                        if (!item.ToString().Contains("$") && (!item.ToString().Contains("Boot")))
                        {
                            DeleteDir(dir.ToString() + "\\" + item.ToString());
                        }
                    }
                }
                Directory.Delete(path);
                return true;
            }
            catch (Exception ex)
            {
                MyLog4NetInfo.ErrorInfo(string.Format("删除报错,当前删除路径{0},错误消息:{1},错误堆栈{2},错误实例{3}",path,ex.Message,ex.StackTrace,ex.InnerException));
                return false;
            }
        }

        /// <summary>
        /// 获取文件路径下的所有文件信息 实体
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static ApplicationEntity GetFiles(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return null;
            ApplicationEntity appEntity = new ApplicationEntity();
            appEntity.MDir = new MDirs();
            appEntity.MDir.Name = path.Substring(path.LastIndexOf("\\") + 1);
            appEntity.MDir.FullName = path;
            appEntity.RelativePath = "\\" + appEntity.MDir.Name;
            appEntity.AbsoulateRootPath = path.Substring(0, path.LastIndexOf("\\") + 1);
            int fileCount = 0;
            GetFilesTree(path, appEntity.MDir, appEntity.AbsoulateRootPath,ref fileCount);
            appEntity.FileCount = fileCount;
            return appEntity;
        }

        private static void GetFilesTree(string targetDir, MDirs currentObject, string absolutePath,ref int fileCount)
        {
            if (currentObject == null)
                currentObject = new MDirs();
            if (currentObject.Files == null)
                currentObject.Files = new List<MFiles>();
            if (currentObject.Dirs == null)
                currentObject.Dirs = new List<MDirs>();
            var length = targetDir.LastIndexOf("\\");
            var rootName = targetDir.Substring(length + 1);
            currentObject.Name = rootName;
            foreach (string fileName in Directory.GetFiles(targetDir))
            {
                var fileInfo = new FileInfo(fileName);
                FileVersionInfo myFileVersion = FileVersionInfo.GetVersionInfo(fileName);
                var version = string.Empty;
                if (myFileVersion != null && !string.IsNullOrEmpty(myFileVersion.FileVersion))
                    version = myFileVersion.FileVersion;
                currentObject.Files.Add(new MFiles
                {
                    Name = fileName.Substring(fileName.LastIndexOf("\\") + 1),
                    ParentName = targetDir,
                    FullName = fileName,
                    ExtendName = fileName.Substring(fileName.LastIndexOf(".") + 1),
                    Size = fileInfo.Length.ToString(),
                    Version = version,
                    AbsoulateRootPath = absolutePath,
                    RelativePath = fileName.Substring(absolutePath.Length - 1)
                });
                fileCount++;
            }
            foreach (string directory in Directory.GetDirectories(targetDir))
            {
                var currentEntity = new MDirs
                {
                    Name = directory.Substring(directory.LastIndexOf("\\") + 1),
                    ParentName = targetDir,
                    FullName = directory,
                    AbsoulateRootPath = absolutePath,
                    RelativePath = directory.Substring(absolutePath.Length - 1)
                };
                currentObject.Dirs.Add(currentEntity);
                GetFilesTree(directory, currentEntity, absolutePath,ref fileCount);
            }
        }

        /// <summary>
        /// 将一个对象序列化为XML
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="txtName"></param>
        public static void XMLSerializer<T>(T t,string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (StreamWriter sw = File.CreateText(path))
            {
                serializer.Serialize(sw, t);
            }
        }

        /// <summary>
        /// 将一个字符串反序列化为指定类型
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="xml">字符串</param>
        /// <returns></returns>
        public static T XMLDeserialize<T>(string xml)
        {
            T t = default(T);
            using (StringReader sdr = new StringReader(xml))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(T));
                    t = (T)serializer.Deserialize(sdr);
                }
                catch (Exception ex)
                {
                   MyLog4NetInfo.LogInfo(string.Format("反序列化报错，错误XML:{0},错误信息:{1},错误堆栈:{2},错误实例:{3}",xml,ex.Message,ex.StackTrace,ex.InnerException));
                }
               
            }
            return t;
        }

        public static T GetTFromXML<T>(string path)
        {      
            if (!string.IsNullOrEmpty(path))
            {
               return XMLDeserialize<T>(path);
            }
            return default(T);
        }

        public static List<string> GetFilesFullNameList(string fileFormat)
        {
            if (string.IsNullOrEmpty(fileFormat))
                return null;
            var pdfFilesPath = AppInfoXMLPath();
            DirectoryInfo folder = new DirectoryInfo(pdfFilesPath);
            var temp = folder.GetFiles(fileFormat);
            var fileDataList = new List<string>();
            if (temp != null && temp.Any())
                temp.ToList().ForEach(c =>
                {
                    fileDataList.Add(c.FullName);
                });
            return fileDataList;
        }

        /// <summary>
        /// 从txt中获取项目信息
        /// </summary>
        /// <returns></returns>
        public static List<ApplicationEntity> GetAppListFromTxt()
        {
            var appList=new List<ApplicationEntity>();
            var fileList = GetFilesFullNameList("*.txt");
            if (fileList != null && fileList.Any())
                fileList.ForEach(o => {
                    var getTxtString = File.ReadAllText(o, Encoding.Default);
                    var appEntity = FileProcessingHelper.GetTFromXML<ApplicationEntity>(getTxtString);
                    if (appEntity != null)
                    {
                        appList.Add(appEntity);
                    }
                });             
            return appList;
        } 
    }
}