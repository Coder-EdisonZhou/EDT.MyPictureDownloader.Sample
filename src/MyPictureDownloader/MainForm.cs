using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace MyPictureDownloader
{
    public partial class FrmMain : Form
    {
        private string destDir; // 目标文件夹
        private int sumCount; // 下载图片总数

        public FrmMain()
        {
            InitializeComponent();
            InitializeEvents();
        }

        private void InitializeEvents()
        {
            this.progressBar.Minimum = 0;
            this.progressBar.Maximum = 100;
            this.btnChoose.Click += btnChoose_Click;
            this.btnStart.Click += btnStart_Click;
            this.btnClose.Click += btnClose_Click;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("您确定要退出本程序？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            string keyword = txtKeyWord.Text.Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                MessageBox.Show("请输入要搜索的关键词！");
                return;
            }

            destDir = txtSavePath.Text.Trim();
            if (string.IsNullOrEmpty(destDir))
            {
                MessageBox.Show("请选择要保存的文件夹！");
                return;
            }
            if (!destDir.EndsWith("\\"))
            {
                destDir += "\\";
            }

            btnStart.Enabled = false;
            if (!string.IsNullOrEmpty(txtLogs.Text))
            {
                if (progressBar.Value > 0)
                {
                    progressBar.Value = progressBar.Minimum;
                }
                txtLogs.Clear();
            }
            // 声明一个异步委托去处理图片下载操作
            Action downloadAction = new Action(() =>
            {
                ProcessDownload(keyword);
            });
            // 声明一个下载完成后的回调函数
            AsyncCallback callBack = new AsyncCallback(asyncResult =>
            {
                downloadAction.EndInvoke(asyncResult);
                progressBar.BeginInvoke(new Action(() =>
                {
                    progressBar.Value = progressBar.Maximum;
                }));
                txtLogs.BeginInvoke(new Action(() =>
                {
                    txtLogs.AppendText("下载图片操作结束！" + Environment.NewLine);
                }));
                btnStart.BeginInvoke(new Action(() =>
                {
                    btnStart.Enabled = true;
                }));
            });
            // 执行该异步委托
            IAsyncResult result = downloadAction.BeginInvoke(callBack, null);
            // 主线程继续干自己的事儿
            txtLogs.AppendText("正在下载图片中..." + Environment.NewLine);
        }

        private void ProcessDownload(string keyword)
        {
            int pageCount = (int)numPageCount.Value;
            sumCount = pageCount * 60;
            for (int i = 0; i < pageCount; i++)
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create("http://image.baidu.com/i?tn=resultjsonavatarnew&ie=utf-8&word=" + Uri.EscapeDataString(keyword) + "&pn=" + pageCount * 60 + "&cg=girl&rn=60&itg=0&lm=-1&ic=0&s=0");
                //HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create("http://image.baidu.com/i?tn=resultjsonavatarnew&ie=utf-8&word=" + Uri.EscapeDataString(keyword) + "&cg=star&pn=" + pageCount * 60 + "&rn=60&z=&itg=0&fr=&width=&height=&lm=-1&ic=0&s=0");

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        using (Stream stream = response.GetResponseStream())
                        {
                            try
                            {
                                // 下载指定页的所有图片
                                DownloadPage(stream);
                            }
                            catch (Exception ex)
                            {
                                // 跨线程访问UI线程的txtLogs
                                txtLogs.BeginInvoke(new Action(() =>
                                    {
                                        txtLogs.AppendText(ex.Message + Environment.NewLine);
                                    }));
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("获取第" + pageCount + "页失败：" + response.StatusCode);
                    }
                }
            }
        }

        private void DownloadPage(Stream stream)
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                string jsonData = reader.ReadToEnd();
                // 解析JSON，分析JSON
                JObject objectRoot = JsonConvert.DeserializeObject(jsonData) as JObject;
                JArray imgsArray = objectRoot["imgs"] as JArray;
                for (int i = 0; i < imgsArray.Count; i++)
                {
                    JObject img = imgsArray[i] as JObject;
                    string objUrl = (string)img["objURL"];
                    //txtLogs.AppendText(objUrl + Environment.NewLine); // 测试获取图片路径
                    try
                    {
                        // 下载具体的某一张图片
                        DownloadImage(objUrl);
                        // 更新进度条
                        progressBar.BeginInvoke(new Action(() =>
                            {
                                progressBar.Value = i * 100 / sumCount;
                            }));
                        // 更新文本框
                        txtLogs.BeginInvoke(new Action(() =>
                            {
                                txtLogs.AppendText("已下载：" + objUrl + Environment.NewLine);
                            }));
                    }
                    catch (Exception ex)
                    {
                        // 跨线程访问UI线程的txtLogs控件
                        txtLogs.BeginInvoke(new Action(() =>
                            {
                                txtLogs.AppendText("【异常：" + ex.Message + "】" + Environment.NewLine);
                            }));
                    }
                }
            }
        }

        private void DownloadImage(string objUrl)
        {
            string destFileName = Path.Combine(destDir, Path.GetFileName(objUrl));
            HttpWebRequest request =
                (HttpWebRequest)HttpWebRequest.Create(objUrl);
            // 欺骗服务器判断URLReferer
            request.Referer = "http://image.baidu.com";
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        using (FileStream fileStream = new FileStream(destFileName, FileMode.Create))
                        {
                            stream.CopyTo(fileStream);
                        }
                    }
                }
                else
                {
                    throw new Exception("下载" + objUrl + "失败，错误码：" + response.StatusCode);
                }
            }
        }

        private void btnChoose_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtSavePath.Text = dlg.SelectedPath;
            }
        }
    }
}
