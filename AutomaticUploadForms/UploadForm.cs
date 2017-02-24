using MemexUpdateCommon;
using MemexUpdateCommon.ServiceReference1;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using LogCommon;

namespace AutomaticUploadForms
{ 
    public partial class UploadForm : Form
    {    
        public UploadForm()
        {
            InitializeComponent();
            GetFilesFromTxtForce();
        }  
       
        /// <summary>
        /// 选择上传文件路径 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSelectePath_Click(object sender, EventArgs e)
        {
            DialogResult dr = folderBrowserDialog1.ShowDialog();
            if (dr == DialogResult.OK && !string.IsNullOrEmpty(folderBrowserDialog1.SelectedPath))
                txtBoxLoadPath.Text = folderBrowserDialog1.SelectedPath;
        }

        /// <summary>
        /// 确定上传文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnConfirm_Click(object sender, EventArgs e)
        {
            var uploadFiles = MemexUpateHelper.GetFiles(txtBoxLoadPath.Text);
            progressBar1.Maximum = uploadFiles.FileCount;
            progressBar1.Value = 0;
            progressBar1.Visible = true;
            label4.Visible=true;
            richTextBox1.Text += "开始上传,项目名称:" + textBox3.Text + ", 版本号:" + txtBoxProjectVersion.Text+"\n";
           
            if (uploadFiles != null && uploadFiles.MDir != null&&uploadFiles.FileCount>0)
            {
                
                MemexUpateHelper.DeleteAppInfo(textBox3.Text);

                MemexUpateHelper.DeleteFile(textBox3.Text);

                MemexUpateHelper.WriteLog(string.Format("开始上传项目:{0},版本号{1},项目路径:{2}", textBox3.Text,txtBoxProjectVersion.Text,txtBoxLoadPath.Text),true);

                StartUploadFile(uploadFiles.MDir);
                MemexUpateHelper.WriteLog(string.Format("上传项目{0} 结束",textBox3.Text),true);
                var appParams = new ApplicationInfo { AppName = textBox3.Text, AppVersion = txtBoxProjectVersion.Text, AppPath = txtBoxLoadPath.Text };

                //设置刚上传的项目信息时应该先更新IIS内存中项目信息列表
                GetFilesFromTxtForce();

                MemexUpateHelper.SetApplicationInfo(appParams);               
                //更新项目文件信息到XML中
                MemexUpateHelper.UpdateAppInfo(textBox3.Text);

                GetFilesFromTxtForce();

                progressBar1.Visible = false;
                label4.Visible = false;
                richTextBox1.Text += "上传完成,项目名称:" + textBox3.Text + ", 版本号:" + txtBoxProjectVersion.Text + "\n";
                MessageBox.Show("上传完成！");
            }
           
        }

        #region upload
        //开始上传
        private  void StartUploadFile(MDirs mdirs)
        {
            if (mdirs != null && mdirs.Files != null && mdirs.Files.Any())
                mdirs.Files.ToList().ForEach(o =>
                {
                    label4.Text = "正在上传文件:" + o.FullName;
                    var isTrue = false;
                    if (!string.IsNullOrEmpty(o.ParentName))
                        isTrue = MemexUpateHelper.DirIsExistOrCreate(o.ParentName.Substring(o.AbsoulateRootPath.Length), textBox3.Text);
                    if (isTrue)
                    {
                        Stream sm = new FileStream(o.FullName, FileMode.Open, FileAccess.Read);
                        string message = "";
                        MemexUpateHelper.UpLoadFile(o.RelativePath, textBox3.Text + "\\", sm.Length, sm, out message);
                    }
                    Application.DoEvents();
                    progressBar1.Value++;
                    richTextBox1.Text += "已上传文件:" + o.FullName+"\n";
                });
            if (mdirs != null && mdirs.Dirs != null && mdirs.Dirs.Any())
                mdirs.Dirs.ToList().ForEach(o =>
                {
                    StartUploadFile(o);
                });
        }
        #endregion

        //获取服务器端文件列表
        private void GetFileList()
        {
            var appList = MemexUpateHelper.GetAppList();
            if (appList != null && appList.Any())
                dataGridView1.DataSource = appList;
        }

        /// <summary>
        /// 强制更新应用程序列表
        /// </summary>
        private void GetFilesFromTxtForce()
        {
            var appList = MemexUpateHelper.GetAppListForce();
            if (appList != null && appList.Any())
                dataGridView1.DataSource = appList;
        }

        private void btnGetFilesList_Click(object sender, EventArgs e)
        {
            GetFileList();
        }

        /// <summary>
        /// 选择文件对比
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            var clientApp = MemexUpateHelper.GetFiles(textBox2.Text);
            var serverApp = MemexUpateHelper.GetServerFiles(textBox1.Text);
         
            if (serverApp != null&&clientApp!=null)
            {
                var listDifferent = MemexUpateHelper.GetDifferentFiles(serverApp.MDir, clientApp.MDir);
                if (listDifferent != null && listDifferent.ToList().Any())
                {
                    MemexUpateHelper.WriteLog(string.Format("开始下载项目：{0}的文件", textBox1.Text),true);
                    StartDownFiles(listDifferent);    
                    MemexUpateHelper.WriteLog(string.Format("下载项目：{0}的文件结束",textBox1.Text),true);       
                }
                MessageBox.Show("完成！");
            }
           else
                MessageBox.Show("服务器端不存在要更新的文件！");
            
        }

        #region download
        /// <summary>
        /// 调用WCF下载文件
        /// </summary>
        /// <param name="fileName"></param>
        private  void DownFile(DifferentFile file)
        {
            Stream sm = new MemoryStream();
            var name = file.FullName.Substring(file.AbsoulteRootPath.Length-1);
            var msg = string.Empty;
            var size = 0l;
            var issuccess = MemexUpateHelper.DownLoadFile(name, textBox1.Text, out msg, out size, out sm);
            if (issuccess.IsSuccess)
            {
                if (file.DiffentValue==DifDescription.FileNotExistInClient&&!Directory.Exists(file.ClientFullPath))
                    Directory.CreateDirectory(file.ClientFullPath);
                var filePath = string.Empty;
                filePath = file.DiffentValue == DifDescription.FileNotExistInClient ? file.ClientFullPath + "\\" + file.FilName : file.ClientFullPath;
                  
                byte[] buffer = new byte[size];
                FileStream fs =new FileStream(filePath, FileMode.Create, FileAccess.Write);
                try
                {
                    int count = 0;
                    while ((count = sm.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fs.Write(buffer, 0, count);
                    }
                    fs.Flush();
                    fs.Close();
                }
                catch (Exception ex)
                { 
                   MemexUpateHelper.WriteLog(string.Format("下载文件:{0}，出错误，错误信息:{1},错误堆栈:{2},错误实例:{3}",file.FilName,ex.Message,ex.StackTrace,ex.InnerException),false);
                }
                finally {
                    ((IDisposable)fs).Dispose();
                }
            }
        }

        #region 客户端下载文件方法

        public  void StartDownFiles(List<DifferentFile> listDifferent)
        {
            if (listDifferent != null && listDifferent.Any())
            {
                //客户端需要下载的文件
                var desList = new List<DifDescription> { DifDescription.DirNotExistInClient, DifDescription.FileNotExistInClient, DifDescription.FileSizeInconsistency, DifDescription.FileVersionInconsistency, DifDescription.ForceUpdate,DifDescription.FileNotExistInServer,DifDescription.DirNotExistInServer};
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
                        if (o.DiffentValue == DifDescription.FileNotExistInClient||o.DiffentValue== DifDescription.FileSizeInconsistency||o.DiffentValue== DifDescription.FileVersionInconsistency)
                        {
                            //如果父目录不存在则创建
                            if (!string.IsNullOrEmpty(o.ClientFullPath)&&!Directory.Exists(o.ClientFullPath.Substring(0,o.ClientFullPath.LastIndexOf("\\"))))
                                Directory.CreateDirectory(o.ClientFullPath.Substring(0,o.ClientFullPath.LastIndexOf("\\")));
                            //下载文件
                            DownFile(o);
                        }
                    });
            }
        }

        #endregion

        #endregion
        /// <summary>
        /// 选择客户端文件路径
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult dr = folderBrowserDialog2.ShowDialog();
            if (dr == DialogResult.OK && !string.IsNullOrEmpty(folderBrowserDialog2.SelectedPath))
                textBox2.Text = folderBrowserDialog2.SelectedPath;
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
           var appName= this.dataGridView1.CurrentCell.Value;
            MemexUpateHelper.WriteLog(string.Format("开始删除项目:{0}，删除内存、txt、对应的文件",appName),true);
            GetFilesFromTxtForce();
            MemexUpateHelper.DeleteAppInfo(appName.ToString());
            GetFilesFromTxtForce();
            MemexUpateHelper.WriteLog(string.Format("删除项目{0}结束,删除内存、txt、对应的文件",appName),true);
        }
    }
}
