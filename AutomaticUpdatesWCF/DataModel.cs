using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace AutomaticUpdatesWCF
{

    [DataContract]
    public class DifferentFile
    {
        [DataMember]
        public string ParentName { get; set; }
        [DataMember]
        public string FilName { get; set; }
        [DataMember]
        public string FullName { get; set; }
        [DataMember]
        public FileType Type { get; set; }

        [DataMember]
        public DifDescription DiffentValue { get; set; }

        [DataMember]
        public string ClientFullPath { get; set; }
        [DataMember]
        public string ServiceFullPath { get; set; }

        /// <summary>
        /// 服务器端绝对根目录
        /// </summary>
        [DataMember]
        public string AbsoulteRootPath { get; set; }
    }
    [DataContract]
    public class ApplicationEntity
    {
        [DataMember]
        public string AppName { get; set; }
        [DataMember]
        public string AppVersion { get; set; }
        [DataMember]
        public MDirs MDir { get; set; }
        
        /// <summary>
        /// 相对路径
        /// </summary>
        [DataMember]
        public string RelativePath { get; set; }

        /// <summary>
        /// 绝对根路径
        /// </summary>
        [DataMember]
        public string AbsoulateRootPath { get; set; }

        /// <summary>
        /// 文件数量
        /// </summary>
        [DataMember]
        public int FileCount { get; set; }

    }
    [DataContract]
    public class MFiles
    {
        /// <summary>
        /// 名称
        /// </summary>
        [DataMember]
        public string Name { get; set; }
        /// <summary>
        /// 全名称(路径)
        /// </summary>
        [DataMember]
        public string FullName { get; set; }

        /// <summary>
        /// 相对路径
        /// </summary>
        [DataMember]
        public string RelativePath { get; set; }

        /// <summary>
        /// 绝对根路径
        /// </summary>
        [DataMember]
        public string AbsoulateRootPath { get; set; }

        /// <summary>
        /// 扩展名
        /// </summary>
        [DataMember]
        public string ExtendName { get; set; }
        /// <summary>
        /// 父名称
        /// </summary>
        [DataMember]
        public string ParentName { get; set; }
        /// <summary>
        /// 大小
        /// </summary>
        [DataMember]
        public string Size { get; set; }
        /// <summary>
        /// 版本
        /// </summary>
        [DataMember]
        public string Version { get; set; }

        /// <summary>
        /// 强制更新
        /// </summary>
        [DataMember]
        public bool IsForceUpdate { get; set; }
    }
    [DataContract]
    public class MDirs
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string FullName { get; set; }
        [DataMember]
        public string ParentName { get; set; }

        /// <summary>
        /// 相对路径
        /// </summary>
        [DataMember]
        public string RelativePath { get; set; }


        /// <summary>
        /// 绝对根路径
        /// </summary>
        [DataMember]
        public string AbsoulateRootPath { get; set; }

        [DataMember]
        public bool IsForceUpdate { get; set; }
        [DataMember]
        public List<MFiles> Files { get; set; }
        [DataMember]
        public List<MDirs> Dirs { get; set; }
    }

    [DataContract]
    public class ApplicationInfo
    {
        [DataMember]
        public string AppName { get; set; }
        [DataMember]
        public string AppVersion { get; set; }
        [DataMember]
        public string AppPath { get; set; }
    }


    [DataContract]
    public enum DifDescription
    {
        /// <summary>
        /// 强制更新
        /// </summary>
        [EnumMember]
        ForceUpdate = 1,
        /// <summary>
        /// 文件在客户端不存在
        /// </summary>
        [EnumMember]
        FileNotExistInClient,
        /// <summary>
        /// 文件在服务器端不存在
        /// </summary>
        [EnumMember]
        FileNotExistInServer,
        /// <summary>
        /// 文件夹在客户端不存在
        /// </summary>
        [EnumMember]
        DirNotExistInClient,
        /// <summary>
        /// 文件夹在服务器端不存在
        /// </summary>
        [EnumMember]
        DirNotExistInServer,
        /// <summary>
        /// 文件大小不一致
        /// </summary>
        [EnumMember]
        FileSizeInconsistency,
        /// <summary>
        /// 文件版本不一致
        /// </summary>
        [EnumMember]
        FileVersionInconsistency
    }

    [DataContract]
    public enum FileType
    {
        //文件
        [EnumMember]
        File,
        //文件夹
        [EnumMember]
        Dir
    }


    #region 下载文件相关类型
    [MessageContract]
    public class tmp
    {
        [MessageHeader]
        public string Name { get; set; }
    }

    [MessageContract]
    public class UpFile
    {
        [MessageHeader]
        public long Size { get; set; }

        [MessageHeader]
        public string FileName { get; set; }

        [MessageBodyMember]
        public Stream FileStream { get; set; }

        [MessageHeader]
        public string ProjectName { get; set; }
    }

    [MessageContract]
    public class DlFile
    {
        [MessageHeader]
        public string FileName { get; set; }

        [MessageHeader]
        public string ProjectName { get; set; }
    }

    [MessageContract]
    public class UpFileResult
    {
        public UpFileResult()
        { }

        public UpFileResult(bool success, string msg)
        {
            IsSuccess = success;
            Message = msg;
        }

        [MessageHeader]
        public bool IsSuccess { get; set; }

        [MessageHeader]
        public string Message { get; set; }
    }

    [MessageContract]
    public class DlFileResult
    {
        [MessageHeader]
        public long Size { get; set; }

        [MessageHeader]
        public string Message { get; set; }

        [MessageHeader]
        public bool IsSuccess { get; set; }

        [MessageBodyMember]
        public Stream FileStream { get; set; }
    }
    #endregion

}