using System.Collections.Generic;
using System.ServiceModel;

namespace AutomaticUpdatesWCF
{
    // 注意: 使用“重构”菜单上的“重命名”命令，可以同时更改代码和配置文件中的接口名“IService1”。
    [ServiceContract]
    public interface IAutomaticUpdateServer
    {
        /// <summary>
        /// 获取应用程序版本
        /// </summary>
        /// <param name="applicationName"></param>
        /// <returns></returns>
        [OperationContract]
        string GetVersionByApplicationName(string applicationName);
        /// <summary>
        /// 获取应用程序文件列表
        /// </summary>
        /// <param name="applicationName"></param>
        /// <returns></returns>
        [OperationContract]
        ApplicationEntity GetServerPublishFiles(string applicationName);
        /// <summary>
        /// 设置应用程序基本信息
        /// </summary>
        /// <param name="appInf"></param>
        [OperationContract]
        bool SetApplicationInfo(ApplicationInfo appInf);

        [OperationContract]
        DlFileResult DownLoadFile(DlFile file);

        [OperationContract]
        UpFileResult UpLoadFile(UpFile file);

        [OperationContract]
        bool DirIsExistOrCreate(string dirName, string projectName);

        [OperationContract]
        List<ApplicationEntity> GetAppList();

        [OperationContract]
        bool DeleteFile(string projectName);

        /// <summary>
        /// 传输数据接口暂时没有方法体
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        List<DifferentFile> GetDifferentList();

        /// <summary>
        /// 文件上传后更新序列化后的XML数据
        /// </summary>
        /// <param name="appName"></param>
        [OperationContract]
        bool UpdateAppInfo(string appName);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="appName"></param>
        /// <returns></returns>
        [OperationContract]
        bool DeleteAppInfo(string appName);

        /// <summary>
        /// 强制更新
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        List<ApplicationEntity> GetAppListFromTxtForce();

        /// <summary>
        /// 调用WCF服务写入日志
        /// </summary>
        /// <param name="message"></param>
        [OperationContract]
        void WriteLog(string message,bool isLog);

        // TODO: 在此添加您的服务操作
    }
}
