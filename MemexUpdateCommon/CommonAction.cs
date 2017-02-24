using MemexUpdateCommon.ServiceReference1;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MemexUpdateCommon
{
    public static class CommonAction
    {
        #region 文件操作(替换、删除)

        #region 私有方法

        /// <summary>
        /// 复制文件到指定目录
        /// </summary>
        /// <param name="sourcePath">源文件路径</param>
        /// <param name="targetPath">目标文件路径</param>
        private static void CopyFiles(string sourcePath, string targetPath)
        {
            File.Copy(sourcePath, targetPath, true);
        }

        /// <summary>
        /// 复制文件夹到指定目录
        /// </summary>
        /// <param name="sourceDirName"></param>
        /// <param name="destDirName"></param>
        /// <param name="total"></param>
        private static void CopyDirectory(string sourceDirName, string destDirName)
        {
            try
            {
                if (!Directory.Exists(destDirName))
                {
                    Directory.CreateDirectory(destDirName);
                    File.SetAttributes(destDirName, File.GetAttributes(sourceDirName));

                }

                if (destDirName[destDirName.Length - 1] != Path.DirectorySeparatorChar)
                    destDirName = destDirName + Path.DirectorySeparatorChar;

                string[] files = Directory.GetFiles(sourceDirName);
                foreach (string file in files)
                {
                    //if (File.Exists(destDirName + Path.GetFileName(file)))
                    //    continue;
                    File.Copy(file, destDirName + Path.GetFileName(file), true);
                    File.SetAttributes(destDirName + Path.GetFileName(file), FileAttributes.Normal);

                }

                string[] dirs = Directory.GetDirectories(sourceDirName);
                foreach (string dir in dirs)
                {
                    CopyDirectory(dir, destDirName + Path.GetFileName(dir));
                }
            }
            catch (Exception ex)
            {
                StreamWriter sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\log.txt", true);
                sw.Write(ex.Message + "     " + DateTime.Now + "\r\n");
                sw.Close();
            }
        }

        /// <summary>
        /// 删除指定目录的文件夹及文件夹下的文件
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static bool DeleteDir(string path)
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
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 删除指定文件
        /// </summary>
        /// <param name="path">文件全名称</param>
        internal static void DeleteFiles(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        #endregion

        #region 公有方法
        /// <summary>
        /// 获取到不同的文件后对文件进行的删除、复制操作
        /// </summary>
        /// <param name="differentFiles"></param>
        internal static void OperationFiles(List<DifferentFile> differentFiles)
        {
            if (differentFiles != null && differentFiles.Any())
                differentFiles.ForEach(o => {
                    switch (o.DiffentValue)
                    {
                        case DifDescription.FileNotExistInClient:
                            CopyFiles(o.FullName, o.ParentName); break;
                        case DifDescription.FileNotExistInServer:
                            DeleteFiles(o.FullName); break;
                        case DifDescription.DirNotExistInClient:
                            CopyDirectory(o.FullName, o.ParentName); break;
                        case DifDescription.DirNotExistInServer:
                            DeleteDir(o.FullName); break;
                    }
                });
        }
        #endregion

        #endregion


        #region 获取并比较文件

        #region 私有方法
        /// <summary>
        /// 获取到指定目录下的所有文件
        /// </summary>
        /// <param name="targetDir"></param>
        /// <param name="currentObject"></param>
        private static void GetFilesTree(string targetDir, MDirs currentObject,string absolutePath,ref int fileCount)
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
                    RelativePath = fileName.Substring(absolutePath.Length-1)
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
                    AbsoulateRootPath=absolutePath,
                    RelativePath=directory.Substring(absolutePath.Length-1)
                };
                currentObject.Dirs.Add(currentEntity);
                GetFilesTree(directory, currentEntity, absolutePath,ref fileCount);
            }
        }

        /// <summary>
        /// 比较文件的差异
        /// </summary>
        /// <param name="server"></param>
        /// <param name="client"></param>
        /// <param name="differentList"></param>
        private static void ComparedFiles(MDirs server, MDirs client, List<DifferentFile> differentList)
        {
            #region 文件对比
            if (server.Files != null && server.Files.Any())
            {
                var serverFiles = server.Files;
                var clientFiles = new List<MFiles>();
                if (client.Files != null && client.Files.Any())
                    clientFiles = client.Files;
                //todo:mh,如果服务器端是强制更新文件或者客户端不存在的文件则客户端文件路径为父级路径,服务器端不存在的文件则全路径也标记为父级路径
                //循环服务器文件
                serverFiles.ForEach(o => {
                    if (o.IsForceUpdate)
                    {
                        differentList.Add(new DifferentFile { DiffentValue = DifDescription.ForceUpdate, ParentName = server.FullName, FilName = o.Name, FullName = o.FullName, Type = FileType.File, ServiceFullPath = o.FullName, ClientFullPath = client.FullName,AbsoulteRootPath=o.AbsoulateRootPath });
                    }
                    else
                    {
                        var firstOrDefault = clientFiles.FirstOrDefault(c => c.Name == o.Name);
                        if (firstOrDefault == null)
                            differentList.Add(new DifferentFile { DiffentValue = DifDescription.FileNotExistInClient, ParentName = server.FullName, FilName = o.Name, FullName = o.FullName, Type = FileType.File, ServiceFullPath = o.FullName, ClientFullPath = client.FullName,AbsoulteRootPath=o.AbsoulateRootPath });
                        else if (firstOrDefault.Size != o.Size)
                            differentList.Add(new DifferentFile { DiffentValue = DifDescription.FileSizeInconsistency, ParentName = server.FullName, FilName = o.Name, FullName = o.FullName, Type = FileType.File, ServiceFullPath = o.FullName, ClientFullPath = firstOrDefault.FullName,AbsoulteRootPath=o.AbsoulateRootPath });
                        else if (!string.IsNullOrEmpty(firstOrDefault.Version) && !string.IsNullOrEmpty(o.Version) && firstOrDefault.Version != o.Version)
                            differentList.Add(new DifferentFile { DiffentValue = DifDescription.FileVersionInconsistency, ParentName = server.FullName, FilName = o.Name, FullName = o.FullName, Type = FileType.File, ServiceFullPath = o.FullName, ClientFullPath = firstOrDefault.FullName,AbsoulteRootPath=o.AbsoulateRootPath });
                    }
                });
                //找出客户端存在而服务器不存在的文件
                if (clientFiles.Any())
                {
                    var serverNotExist = clientFiles.Where(o => !serverFiles.Select(c => c.Name).Contains(o.Name)).ToList();
                    if (serverNotExist.Any())
                        serverNotExist.ForEach(o => {
                            differentList.Add(new DifferentFile { DiffentValue = DifDescription.FileNotExistInServer, ParentName = server.FullName, FilName = o.Name, FullName = o.FullName, Type = FileType.File, ServiceFullPath = server.FullName, ClientFullPath = o.FullName,AbsoulteRootPath=server.AbsoulateRootPath });
                        });
                }
            }
            //如果服务器端没文件
            else if (client.Files != null && client.Files.Any())
            {
                var clientFiles = client.Files;
                if (clientFiles.Any())
                    clientFiles.ForEach(o => {
                        differentList.Add(new DifferentFile { DiffentValue = DifDescription.FileNotExistInServer, ParentName = client.FullName, FilName = o.Name, FullName = o.FullName, Type = FileType.File, ServiceFullPath = server.FullName, ClientFullPath = o.FullName,AbsoulteRootPath=server.AbsoulateRootPath });
                    });
            }
            #endregion

            #region 文件夹对比
            if (server.Dirs != null && server.Dirs.Any())
            {
                var serverDirs = server.Dirs;
                var clientDirs = new List<MDirs>();
                if (client.Dirs.Any())
                    clientDirs = client.Dirs;
                //循环服务器文件夹
                serverDirs.ForEach(o => {
                    var firstOrDefault = clientDirs.FirstOrDefault(c => c.Name == o.Name);
                    if (firstOrDefault == null)
                    {
                        differentList.Add(new DifferentFile { DiffentValue = DifDescription.DirNotExistInClient, ParentName = server.FullName, FilName = o.Name, FullName = o.FullName, Type = FileType.Dir, ServiceFullPath = o.FullName, ClientFullPath = client.FullName ,AbsoulteRootPath=o.AbsoulateRootPath});
                        if ((o.Files != null && o.Files.Any()) || (o.Dirs != null && o.Dirs.Any()))
                            DifferentDirAndSub(o, differentList, client.FullName);
                    }
                    else
                        ComparedFiles(o, firstOrDefault, differentList);
                });
                //找到客户端存在而服务器端不存在的文件夹
                if (clientDirs.Any())
                {
                    var serverNotExist = clientDirs.Where(o => !serverDirs.Select(c => c.Name).Contains(o.Name)).ToList();
                    if (serverNotExist.Any())
                        serverNotExist.ForEach(o => {
                            differentList.Add(new DifferentFile { DiffentValue = DifDescription.DirNotExistInServer, ParentName = server.FullName, FilName = o.Name, FullName = o.FullName, Type = FileType.Dir, ServiceFullPath = server.FullName, ClientFullPath = o.FullName });
                        });
                }
            }
            //如果服务器端没有文件夹
            else if (client.Dirs != null && client.Dirs.Any())
            {
                var clientDirs = client.Dirs;
                var serverDirs = server.Dirs;
                var serverNotExist = clientDirs.Where(o => !serverDirs.Select(c => c.Name).Contains(o.Name)).ToList();
                if (serverNotExist != null && serverNotExist.Any())
                    serverNotExist.ForEach(o => {
                        differentList.Add(new DifferentFile { DiffentValue = DifDescription.DirNotExistInServer, ParentName = client.FullName, FilName = o.Name, FullName = o.FullName, Type = FileType.Dir, ServiceFullPath = server.FullName, ClientFullPath = o.FullName });
                    });
            }
            #endregion
        }

        /// <summary>
        /// 循环文件夹下的所有文件和文件夹，客户端不存在的
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="differentFileList"></param>
        private static void DifferentDirAndSub(MDirs dir, List<DifferentFile> differentFileList, string clientFullName)
        {
            if (dir.Files != null && dir.Files.Any())
            {
                dir.Files.ForEach(o => {
                    differentFileList.Add(new DifferentFile { DiffentValue = DifDescription.FileNotExistInClient, FilName = o.Name, FullName = o.FullName, ParentName = o.ParentName, Type = FileType.File, ServiceFullPath = o.FullName, ClientFullPath = clientFullName + "\\" + dir.Name,AbsoulteRootPath=o.AbsoulateRootPath });
                });
            }
            if (dir.Dirs != null && dir.Dirs.Any())
            {
                dir.Dirs.ForEach(o => {
                    differentFileList.Add(new DifferentFile { DiffentValue = DifDescription.DirNotExistInClient, FilName = o.Name, FullName = o.FullName, ParentName = o.ParentName, Type = FileType.Dir, ServiceFullPath = o.FullName, ClientFullPath = clientFullName + "\\" + dir.Name + "\\" + o.Name,AbsoulteRootPath=o.AbsoulateRootPath });

                    DifferentDirAndSub(o, differentFileList, clientFullName + "\\" + dir.Name + "\\" + o.Name);
                });
            }
        }

        #endregion

        #region 公有方法
        /// <summary>
        /// 获取到指定目录下的所有文件
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static ApplicationEntity GetFiles(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return null;
            ApplicationEntity appEntity = new ApplicationEntity();
            appEntity.MDir = new MDirs();
            appEntity.MDir.Name = path.Substring(path.LastIndexOf("\\") + 1);
            appEntity.MDir.FullName = path;
            appEntity.RelativePath = "\\"+appEntity.MDir.Name;
            appEntity.AbsoulateRootPath= path.Substring(0, path.LastIndexOf("\\") + 1);
            int fileCount = 0;     
            GetFilesTree(path, appEntity.MDir, appEntity.AbsoulateRootPath,ref fileCount);
            appEntity.FileCount = fileCount;
            return appEntity;
        }
        /// <summary>
        /// 比较文件的差异
        /// </summary>
        /// <param name="server"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        internal static List<DifferentFile> GetDifferentFiles(MDirs server, MDirs client)
        {
            var differentFileList = new List<DifferentFile>();
            ComparedFiles(server, client, differentFileList);
            return differentFileList;
        }

       
        #endregion

        #endregion

        private static string GetUpLoadFilePath()
        {
            string path = System.AppDomain.CurrentDomain.BaseDirectory + @"UpLoadFile\";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }
    }
}
