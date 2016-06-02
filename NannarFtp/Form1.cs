using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace NannarFtp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        FtpClient ftp;
        const string UpLevelString = "上级目录...";

        public FtpConfig GetFtpConfig()
        {
            FtpConfig config = new FtpConfig();
            config.ServerIP = textBox1.Text.Trim();
            config.UserName = textBox2.Text.Trim();
            config.Password = textBox3.Text.Trim();
            config.UseSSL = checkBox1.Checked;
            config.RemotePath = textBox4.Text.Trim();
            config.UsePassive = checkBox3.Checked;
            config.UseBinary = checkBox4.Checked;
            return config;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            FtpConfig config = GetFtpConfig();
            toolStripStatusLabel1.Text = ftp.Test(config);
            MessageBox.Show(toolStripStatusLabel1.Text);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (button4.Text == "连接")
                Connect();
            else
                Disconnect();
        }

        private void Disconnect()
        {
            try
            {
                if (ftp != null)
                {
                    ftp.Close();
                    textBox4.Text = ftp.CurrentPath;
                    listView2.Items.Clear();
                }
                textBox5.Enabled = false;
                button4.Text = "连接";
            }
            catch (Exception e)
            {
                MessageBox.Show("断开失败：" + e.Message);
            }
        }

        private void Connect()
        {
            try
            {
                FtpConfig config = GetFtpConfig();
                if (ftp != null)
                {
                    ftp.Connect(config);
                    if (RefreshRemoteFiles())
                    {
                        textBox5.Enabled = true;
                        button4.Text = "断开";
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("连接失败：" + e.Message);
            }
        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            RefreshRemoteFiles();
        }

        private bool RefreshRemoteFiles()
        {
            try
            {
                listView2.Items.Clear();
                string regExp = textBox5.Text.Trim();
                string[] files = ftp.GetList(regExp);
                foreach (string f in files)
                {
                    int index = f.IndexOf("<DIR>", StringComparison.InvariantCultureIgnoreCase);
                    if (index > 0)
                        listView2.Items.Add(f, 1);
                    else
                        listView2.Items.Add(f, 0);
                }
                string dir = ftp.CurrentPath;
                if (!string.IsNullOrEmpty(dir))
                {
                    listView2.Items.Insert(0, UpLevelString, 1);
                }
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show("获取列表出现异常：" + e.Message);
            }
            return false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                string locDir = textBox6.Text.Trim();
                if (Directory.Exists(locDir))
                {
                    toolStripProgressBar1.Visible = true;
                    toolStripProgressBar1.Maximum = listView1.SelectedItems.Count;
                    int i = 0;
                    List<string> names = new List<string>();
                    foreach (ListViewItem lvi in listView1.SelectedItems)
                    {
                        if (lvi.Text != UpLevelString)
                        {
                            bool isDir;
                            string name = GetName(lvi.Text, out isDir);
                            if (isDir)
                            {
                                GetFilesByDirLocal(locDir + "\\" + name);
                            }
                            else
                            {
                                ftp.Upload(locDir + "\\" + name);
                            }
                            i++;
                            toolStripProgressBar1.Value = i;
                            statusStrip1.Refresh();
                        }
                    }
                    EnterDirLocalAssign(locDir);
                    toolStripProgressBar1.Visible = false;
                }
                else
                {
                    MessageBox.Show("本地目录[" + locDir + "]不存在！上传终止。");
                }
            }
            catch (Exception ex)
            {
                toolStripProgressBar1.Visible = false;
                MessageBox.Show("上传时出现异常：" + ex.Message);
            }
        }

        private void GetFilesByDirLocal(string dir)
        {
            DirectoryInfo di = new DirectoryInfo(dir);
            EnterDirLocal(di.Name);
            EnterDirRemote(di.Name);
            string[] files = Directory.GetFiles(dir);
            string[] dirs = Directory.GetDirectories(dir);
            foreach (string file in files)
            {
                FileInfo fi = new FileInfo(file);
                ftp.Upload(dir + "\\" + fi.Name);
            }
            foreach (string d in dirs)
            {
                GetFilesByDirLocal(d);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                string locDir = textBox6.Text.Trim();
                if (Directory.Exists(locDir))
                {
                    toolStripProgressBar1.Visible = true;
                    toolStripProgressBar1.Maximum = listView2.SelectedItems.Count;
                    int i = 0;
                    foreach (ListViewItem lvi in listView2.SelectedItems)
                    {
                        if (lvi.Text != UpLevelString)
                        {
                            bool isDir;
                            string name = GetName(lvi.Text, out isDir);
                            if (isDir)
                            {
                                GetFilesByDirRemote(name);
                            }
                            else
                            {
                                ftp.Download(locDir, name);
                            }
                            i++;
                            toolStripProgressBar1.Value = i;
                            statusStrip1.Refresh();
                        }
                    }
                    EnterDirLocalAssign(locDir);
                    toolStripProgressBar1.Visible = false;
                }
                else
                {
                    MessageBox.Show("本地目录[" + locDir + "]不存在！下载终止。");
                }
            }
            catch (Exception ex)
            {
                toolStripProgressBar1.Visible = false;
                MessageBox.Show("下载时出现异常：" + ex.Message);
            }
        }

        private void GetFilesByDirRemote(string dirName)
        {
            EnterDirLocal(dirName);
            EnterDirRemote(dirName);
            List<string> dirs = new List<string>();
            foreach (ListViewItem lvi in listView2.Items)
            {
                string rawStr = lvi.Text;
                if (rawStr != UpLevelString)
                {
                    bool isDir;
                    string name = GetName(rawStr, out isDir);
                    if (name != "")
                    {
                        if (isDir)
                            dirs.Add(name);
                        else
                            ftp.Download(this.textBox6.Text, name);
                    }
                }
            }
            foreach (string d in dirs)
            {
                GetFilesByDirRemote(d);
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            textBox2.Enabled = textBox3.Enabled = !checkBox2.Checked;
        }

        private void listView1_DragEnter(object sender, DragEventArgs e)
        {

        }

        private void listView1_DragDrop(object sender, DragEventArgs e)
        {

        }

        private void listView2_DragEnter(object sender, DragEventArgs e)
        {

        }

        private void listView2_DragDrop(object sender, DragEventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            System.Net.IPAddress ip = Network.GetLocalIPv4("");
            textBox1.Text = ip.ToString();
            LoadLocDir();
            ftp = new FtpClient();
        }

        private void LoadLocDir()
        {
            string locDir = textBox6.Text.Trim();
            if (Directory.Exists(locDir))
            {
                listView1.Items.Clear();
                string[] ss = Directory.GetFileSystemEntries(locDir);
                foreach (string s in ss)
                {
                    if (Directory.Exists(s))
                    {
                        DirectoryInfo di = new DirectoryInfo(s);
                        DateTime modifyTime = di.LastWriteTime;
                        listView1.Items.Add(modifyTime.ToString("yyyy-MM-dd HH:mm:ss") + " <DIR> " + new string(' ', 9) + di.Name, 1);
                    }
                    else
                    {
                        FileInfo fi = new FileInfo(s);
                        DateTime modifyTime = fi.LastWriteTime;
                        listView1.Items.Add(modifyTime.ToString("yyyy-MM-dd HH:mm:ss") + " " + fi.Length.ToString().PadLeft(14, ' ') + " " + fi.Name, 0);
                    }
                }
                DirectoryInfo dii = Directory.GetParent(locDir);
                if (dii != null)
                {
                    listView1.Items.Insert(0, UpLevelString, 1);
                }
            }
        }

        private void 重命名ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedItems != null && listView2.SelectedItems.Count > 0)
            {
                string rawStr = listView2.SelectedItems[0].Text;
                if (rawStr != UpLevelString)
                {
                    bool isDir;
                    string name = GetName(rawStr, out isDir);
                    if (name != "")
                    {
                        string s = "文件";
                        if (isDir)
                            s = "目录";
                        Form2 newName = new Form2();
                        newName.SetOldName(name);
                        if (newName.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            try
                            {
                                bool flag = ftp.Rename(name, newName.NewName);
                                if (flag)
                                {
                                    MessageBox.Show("重命名成功！");
                                    RefreshRemoteFiles();
                                }
                                else
                                {
                                    MessageBox.Show("重命名失败！");
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("重命名" + name + s + "时出现异常：" + ex.Message);
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("选定的文件(目录)名称为空！");
                }
            }
        }

        private static string GetName(string rawStr, out bool isDir)
        {
            isDir = false;
            string name = "";
            int index = rawStr.IndexOf("<DIR>", StringComparison.InvariantCultureIgnoreCase);
            if (index > 0 && index + 5 < rawStr.Length)
            {
                name = rawStr.Substring(index + 5).Trim();
                isDir = true;
            }
            else
            {
                string s = rawStr;
                if (s.Length > 19)
                {
                    s = s.Substring(19).Trim();
                    int a = s.IndexOf(" ");
                    if (a > 0 && a < s.Length - 1)
                    {
                        name = s.Substring(a + 1);
                    }
                }
            }
            return name;
        }

        private void 删除ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedItems != null && listView2.SelectedItems.Count > 0)
            {
                if (MessageBox.Show("确定要删除这些远程文件(目录)？", "删除提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.OK)
                {
                    List<string> failtures = new List<string>();
                    foreach (ListViewItem lvi in listView2.SelectedItems)
                    {
                        string name = "";
                        string rawStr = lvi.Text;
                        if (rawStr != UpLevelString)
                        {
                            try
                            {
                                int index = rawStr.IndexOf("<DIR>", StringComparison.InvariantCultureIgnoreCase);
                                if (index > 0 && index + 5 < rawStr.Length)
                                {
                                    string dirname = rawStr.Substring(index + 5).Trim();
                                    if (dirname != "")
                                    {
                                        name = dirname;
                                        bool flag = ftp.RemoveDirectory(dirname);
                                        if (!flag) failtures.Add("目录<" + dirname + ">");
                                    }
                                }
                                else
                                {
                                    string s = lvi.Text;
                                    if (s.Length > 19)
                                    {
                                        s = s.Substring(19).Trim();
                                        int a = s.IndexOf(" ");
                                        if (a > 0 && a < s.Length - 1)
                                        {
                                            string file = s.Substring(a + 1);
                                            name = file;
                                            bool flag = ftp.DeleteFile(file);
                                            if (!flag) failtures.Add(file);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("删除" + name + "时出现异常：" + ex.Message);
                                failtures.Add(name);
                            }
                        }
                        else
                        {
                            MessageBox.Show("选定的文件(目录)为空！");
                        }
                        if (failtures.Count > 0)
                        {
                            MessageBox.Show("删除完毕！其中有" + failtures.Count + "个文件(目录)删除失败：" + Environment.NewLine + string.Join(",", failtures.ToArray()));
                        }
                        else
                        {
                            MessageBox.Show("选定的文件(目录)全部删除成功！");
                        }
                        RefreshRemoteFiles();
                    }
                }
            }
            else
            {
                MessageBox.Show("请先选定要删除的文件(目录)！");
            }
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewItem lvi = listView1.HitTest(e.Location).Item;
            if (lvi != null)
            {
                if (lvi.Index == 0 && lvi.Text == UpLevelString)
                {
                    UpLevelLocal();
                }
                else if (lvi.Text.IndexOf("<DIR>", StringComparison.InvariantCultureIgnoreCase) > 0)
                {
                    int index = lvi.Text.IndexOf("<DIR>", StringComparison.InvariantCultureIgnoreCase);
                    string name = lvi.Text.Substring(index + 5).Trim();
                    EnterDirLocal(name);
                }
            }
        }

        private void EnterDirRemote(string name)
        {
            if (!string.IsNullOrEmpty(name))
                textBox4.Text = ftp.EnterPath(name);
            textBox4.Refresh();
            RefreshRemoteFiles();
        }

        private void EnterDirLocal(string name)
        {
            string locDir = textBox6.Text.Trim();
            if (!string.IsNullOrEmpty(name))
                textBox6.Text = locDir + "\\" + name;
            textBox6.Refresh();
            if (!Directory.Exists(textBox6.Text))
                Directory.CreateDirectory(textBox6.Text);
            LoadLocDir();
        }

        private void EnterDirLocalAssign(string dir)
        {
            if (!string.IsNullOrEmpty(dir))
                textBox6.Text = dir;
            if (!Directory.Exists(textBox6.Text))
                Directory.CreateDirectory(textBox6.Text);
            LoadLocDir();
        }

        private void UpLevelLocal()
        {
            string locDir = textBox6.Text.Trim();
            DirectoryInfo di = Directory.GetParent(locDir);
            if (di != null)
            {
                if (di.FullName.EndsWith("\\"))
                    textBox6.Text = di.FullName.Substring(0, di.FullName.Length - 1);
                else
                    textBox6.Text = di.FullName;
                LoadLocDir();
            }
        }

        private void listView2_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewItem lvi = listView2.HitTest(e.Location).Item;
            if (lvi != null)
            {
                if (lvi.Index == 0 && lvi.Text == UpLevelString)
                {
                    UpLevelRemote();
                }
                else if (lvi.Text.IndexOf("<DIR>", StringComparison.InvariantCultureIgnoreCase) > 0)
                {
                    int index = lvi.Text.IndexOf("<DIR>", StringComparison.InvariantCultureIgnoreCase);
                    string dirname = lvi.Text.Substring(index + 5).Trim();
                    if (dirname != "")
                        EnterDirRemote(dirname);
                }
            }
        }

        private void UpLevelRemote()
        {
            string remoteDir = textBox4.Text.Trim();
            if (remoteDir != "")
            {
                textBox4.Text = ftp.UpPath();                
                RefreshRemoteFiles();
            }
        }
    }
}
