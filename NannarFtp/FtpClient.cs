using System;
using System.Text;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Net.Security;
namespace NannarFtp
{
    public struct FtpConfig
    {
        /// <summary>
        /// FTP连接地址
        /// </summary>
        public string ServerIP;
        /// <summary>
        /// 指定FTP远程目录
        /// </summary>
        public string RemotePath;
        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName;
        /// <summary>
        /// 密码
        /// </summary>
        public string Password;
        /// <summary>
        /// 使用二进制传输
        /// </summary>
        public bool UseBinary;
        /// <summary>
        /// 使用被动传输
        /// </summary>
        public bool UsePassive;
        /// <summary>
        /// 使用SSL加密
        /// </summary>
        public bool UseSSL;
    }
    public class FtpClient
    {
        int bufferSize = 2048;

        public int BufferSize
        {
            get { return bufferSize; }
            set { bufferSize = value; }
        }
        FtpConfig config;
        string ftpURI;
        FtpWebRequest request;
        /// <summary>
        /// 连接FTP服务器的客户端构造函数
        /// </summary>
        /// <param name="autoSignature">自签名证书</param>
        public FtpClient(bool autoSignature = true)
        {
            if (autoSignature)
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback((a, b, c, d) => { return true; });
        }
        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="ftpConfig">FTP配置信息</param>
        /// <returns></returns>
        public bool Connect(FtpConfig ftpConfig)
        {
            WebResponse response = null;
            this.config = ftpConfig;
            ftpURI = "ftp://" + config.ServerIP;
            if (!string.IsNullOrEmpty(config.RemotePath))
                ftpURI += "/" + config.RemotePath;
            request = (FtpWebRequest)FtpWebRequest.Create(ftpURI);
            request.UseBinary = config.UseBinary;
            request.UsePassive = config.UsePassive;
            request.EnableSsl = config.UseSSL;
            request.Credentials = new NetworkCredential(config.UserName, config.Password);
            try
            {
                response = request.GetResponse();
                return true;
            }
            catch { }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
            }
            return false;
        }
        /// <summary>
        /// 断开连接
        /// </summary>
        /// <returns></returns>
        public bool Close()
        {
            try
            {
                config.RemotePath = "";
                ftpURI = "ftp://" + config.ServerIP;
                if (request != null)
                {
                    request.Abort();
                    Stream s = request.GetRequestStream();
                    s.Close();
                    WebResponse r = request.GetResponse();
                    r.Close();
                }
                return true;
            }
            catch { }
            return false;
        }
        /// <summary>
        /// 上传
        /// </summary>
        /// <param name="filename">本地文件路径名称</param>
        /// <param name="uniqueName">唯一名称</param>
        public void Upload(string filename, bool uniqueName = false)
        {
            FileInfo file = new FileInfo(filename);
                if (request != null)
                {
                    request.Abort();
                }
            request = (FtpWebRequest)FtpWebRequest.Create(new Uri(ftpURI + "/" + file.Name));
            request.Credentials = new NetworkCredential(config.UserName, config.Password);
            //request.KeepAlive = false;
            if (uniqueName)
                request.Method = WebRequestMethods.Ftp.UploadFileWithUniqueName;
            else
                request.Method = WebRequestMethods.Ftp.UploadFile;
            request.UseBinary = config.UseBinary;
            request.UsePassive = config.UsePassive;
            request.EnableSsl = config.UseSSL;
            request.ContentLength = file.Length;
            byte[] buff = new byte[bufferSize];
            int readCount;
            FileStream fs = file.OpenRead();
            Stream strm = null;
            try
            {
                strm = request.GetRequestStream();
                readCount = fs.Read(buff, 0, bufferSize);
                while (readCount != 0)
                {
                    strm.Write(buff, 0, readCount);
                    readCount = fs.Read(buff, 0, bufferSize);
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally
            {
                if (request != null)
                {
                    request.Abort();
                }
                request = (FtpWebRequest)FtpWebRequest.Create(ftpURI);
                request.Credentials = new NetworkCredential(config.UserName, config.Password);
                request.UseBinary = config.UseBinary;
                request.UsePassive = config.UsePassive;
                request.EnableSsl = config.UseSSL;
                if (strm != null)
                {
                    strm.Close();
                }
                if (fs != null)
                {
                    fs.Close();
                }
            }
        }
        /// <summary>
        /// 下载
        /// </summary>
        /// <param name="filePath">本地保存文件路径</param>
        /// <param name="fileName">服务器文件名</param>
        public void Download(string filePath, string fileName)
        {
            FileStream outputStream = null;
            FtpWebResponse response = null;
            Stream ftpStream = null;
            try
            {
                if (request != null)
                {
                    request.Abort();
                }
                outputStream = new FileStream(filePath + "\\" + fileName, FileMode.Create);
                request = (FtpWebRequest)FtpWebRequest.Create(new Uri(ftpURI + "/" + fileName));
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.UseBinary = config.UseBinary;
                request.UsePassive = config.UsePassive;
                request.EnableSsl = config.UseSSL;
                request.Credentials = new NetworkCredential(config.UserName, config.Password);
                response = (FtpWebResponse)request.GetResponse();
                ftpStream = response.GetResponseStream();
                long cl = response.ContentLength;
                int readCount;
                byte[] buffer = new byte[bufferSize];
                readCount = ftpStream.Read(buffer, 0, bufferSize);
                while (readCount > 0)
                {
                    outputStream.Write(buffer, 0, readCount);
                    readCount = ftpStream.Read(buffer, 0, bufferSize);
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally
            {
                if (request != null)
                {
                    request.Abort();
                }
                request = (FtpWebRequest)FtpWebRequest.Create(ftpURI);
                request.Credentials = new NetworkCredential(config.UserName, config.Password);
                request.UseBinary = config.UseBinary;
                request.UsePassive = config.UsePassive;
                request.EnableSsl = config.UseSSL;
                if (ftpStream != null)
                {
                    ftpStream.Close();
                }
                if (outputStream != null)
                {
                    outputStream.Close();
                }
                if (response != null)
                {
                    response.Close();
                }
            }
        }

        public string CurrentPath
        {
            get
            {
                return config.RemotePath;
            }
        }

        public string EnterPath(string subDir)
        {
            if (!IsExistsDir(subDir))
            {
                CreateDirectory(subDir);
                config.RemotePath += "/" + subDir;
                ftpURI = "ftp://" + config.ServerIP + "/" + config.RemotePath;
            }
            else
            {
                config.RemotePath += "/" + subDir;
                ftpURI = "ftp://" + config.ServerIP + "/" + config.RemotePath;
            }
            return config.RemotePath;
        }

        private bool IsExistsDir(string subDir)
        {
            FtpWebRequest test = null;
            WebResponse response = null;
            try
            {
                test = (FtpWebRequest)FtpWebRequest.Create(ftpURI + "/" + subDir);
                test.Credentials = new NetworkCredential(config.UserName, config.Password);
                test.Method = WebRequestMethods.Ftp.ListDirectory;
                test.UseBinary = config.UseBinary;
                test.UsePassive = config.UsePassive;
                test.EnableSsl = config.UseSSL;
                response = test.GetResponse();
                return true;
            }
            catch { return false; }
            finally
            {
                if (test != null)
                {
                    test.Abort();
                }
                if (response != null)
                {
                    response.Close();
                }
            }
        }

        public string UpPath()
        {
            config.RemotePath = UpLevelDir;
            ftpURI = "ftp://" + config.ServerIP;
            if (!string.IsNullOrEmpty(config.RemotePath))
                ftpURI += "/" + config.RemotePath;
            return config.RemotePath;
        }

        public string UpLevelDir
        {
            get
            {
                string s = config.RemotePath;
                if (!string.IsNullOrEmpty(s))
                {
                    int index = s.LastIndexOf("/");
                    if (index > 0)
                        return s.Substring(0, index);
                    return string.Empty;
                }
                else
                {
                    return null;
                }
            }
        }
        /// <summary>
        /// 获取当前目录下满足指定正则表达式的文件名列表
        /// </summary>
        /// <param name="regExp">正则表达式</param>
        /// <returns></returns>
        public string[] GetList(string regExp)
        {
            StringBuilder result = new StringBuilder();
            StreamReader reader = null;
            WebResponse response = null;
            try
            {
                if (request != null)
                {
                    request.Abort();
                }
                request = (FtpWebRequest)FtpWebRequest.Create(ftpURI);
                request.Credentials = new NetworkCredential(config.UserName, config.Password);
                request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                request.UseBinary = config.UseBinary;
                request.UsePassive = config.UsePassive;
                request.EnableSsl = config.UseSSL;
                response = request.GetResponse();
                reader = new StreamReader(response.GetResponseStream());
                string line = reader.ReadLine();
                while (line != null)
                {
                    if (regExp.Length != 0)
                    {
                        Regex rg = new Regex(regExp);
                        if (rg.Match(line).Success)
                        {
                            result.Append(line);
                            result.Append("\n");
                        }
                    }
                    else
                    {
                        if (line != "")
                        {
                            result.Append(line);
                            result.Append("\n");
                        }
                    }
                    line = reader.ReadLine();
                }
                // to remove the tailing '\n'
                if (result.Length != 0)
                {
                    result.Remove(result.ToString().LastIndexOf('\n'), 1);
                }
                return result.ToString().Split('\n');
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
                if (reader != null)
                {
                    reader.Close();
                }
            }
        }
        /// <summary>
        /// 删除当前目录下指定的文件
        /// </summary>
        /// <param name="filename">当前目录下指定的文件</param>
        /// <returns></returns>
        public bool DeleteFile(string filename)
        {
            WebResponse response = null;
            try
            {
                if (request != null)
                {
                    request.Abort();
                }
                request = (FtpWebRequest)FtpWebRequest.Create(ftpURI + "/" + filename);
                request.Credentials = new NetworkCredential(config.UserName, config.Password);
                request.Method = WebRequestMethods.Ftp.DeleteFile;
                request.UseBinary = config.UseBinary;
                request.UsePassive = config.UsePassive;
                request.EnableSsl = config.UseSSL;
                response = request.GetResponse();
                return true;
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally
            {
                if (request != null)
                {
                    request.Abort();
                }
                request = (FtpWebRequest)FtpWebRequest.Create(ftpURI);
                request.Credentials = new NetworkCredential(config.UserName, config.Password);
                request.UseBinary = config.UseBinary;
                request.UsePassive = config.UsePassive;
                request.EnableSsl = config.UseSSL;
                if (response != null)
                {
                    response.Close();
                }
            }
        }
        /// <summary>
        /// 删除当前目录下指定的文件
        /// </summary>
        /// <param name="dirname">当前目录下指定的目录</param>
        /// <returns></returns>
        public bool RemoveDirectory(string dirname)
        {
            WebResponse response = null;
            try
            {
                if (request != null)
                {
                    request.Abort();
                }
                request = (FtpWebRequest)FtpWebRequest.Create(ftpURI + "/" + dirname);
                request.Credentials = new NetworkCredential(config.UserName, config.Password);
                request.Method = WebRequestMethods.Ftp.RemoveDirectory;
                request.UseBinary = config.UseBinary;
                request.UsePassive = config.UsePassive;
                request.EnableSsl = config.UseSSL;
                response = request.GetResponse();
                return true;
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally
            {
                if (request != null)
                {
                    request.Abort();
                }
                request = (FtpWebRequest)FtpWebRequest.Create(ftpURI);
                request.Credentials = new NetworkCredential(config.UserName, config.Password);
                request.UseBinary = config.UseBinary;
                request.UsePassive = config.UsePassive;
                request.EnableSsl = config.UseSSL;
                if (response != null)
                {
                    response.Close();
                }
            }
        }
        /// <summary>
        /// 重命名当前目录下指定的文件
        /// </summary>
        /// <param name="oldName">当前目录下指定的文件或目录</param>
        /// <param name="newName">新名称</param>
        /// <returns></returns>
        public bool Rename(string oldName, string newName)
        {
            WebResponse response = null;
            try
            {
                if (request != null)
                {
                    request.Abort();
                }
                request = (FtpWebRequest)FtpWebRequest.Create(ftpURI + "/" + oldName);
                request.Credentials = new NetworkCredential(config.UserName, config.Password);
                request.Method = WebRequestMethods.Ftp.Rename;
                request.RenameTo = newName;
                request.UseBinary = config.UseBinary;
                request.UsePassive = config.UsePassive;
                request.EnableSsl = config.UseSSL;
                response = request.GetResponse();
                return true;
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally
            {
                if (request != null)
                {
                    request.Abort();
                }
                request = (FtpWebRequest)FtpWebRequest.Create(ftpURI);
                request.Credentials = new NetworkCredential(config.UserName, config.Password);
                request.UseBinary = config.UseBinary;
                request.UsePassive = config.UsePassive;
                request.EnableSsl = config.UseSSL;
                if (response != null)
                {
                    response.Close();
                }
            }
        }
        /// <summary>
        /// 断点续传(有待测试检验)
        /// </summary>
        /// <param name="filePath">本地上传文件的目录</param>
        /// <param name="filename">要断点续传的文件名</param>
        /// <param name="position">断点的位置</param>
        /// <returns></returns>
        public bool AppendFile(string filePath, string filename, int position)
        {
            WebResponse response = null;
            try
            {
                if (request != null)
                {
                    request.Abort();
                }
                request = (FtpWebRequest)FtpWebRequest.Create(ftpURI + "/" + filename);
                request.Credentials = new NetworkCredential(config.UserName, config.Password);
                request.Method = WebRequestMethods.Ftp.AppendFile;
                request.UseBinary = config.UseBinary;
                request.UsePassive = config.UsePassive;
                request.EnableSsl = config.UseSSL;
                FileInfo fileInf = new FileInfo(filePath + "\\" + filename);
                request.ContentLength = fileInf.Length - position;
                byte[] buff = new byte[bufferSize];
                int readCount;
                FileStream fs = fileInf.OpenRead();
                Stream strm = null;
                strm = request.GetRequestStream();
                readCount = fs.Read(buff, position, bufferSize);
                while (readCount != 0)
                {
                    strm.Write(buff, 0, readCount);
                    readCount = fs.Read(buff, 0, bufferSize);//这里的offset要不要提升读取位置：position+readCount？
                }
                return true;
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally
            {
                if (request != null)
                {
                    request.Abort();
                }
                request = (FtpWebRequest)FtpWebRequest.Create(ftpURI);
                request.Credentials = new NetworkCredential(config.UserName, config.Password);
                request.UseBinary = config.UseBinary;
                request.UsePassive = config.UsePassive;
                request.EnableSsl = config.UseSSL;
                if (response != null)
                {
                    response.Close();
                }
            }
        }
        /// <summary>
        /// 创建新目录
        /// </summary>
        /// <param name="dirName"></param>
        /// <returns></returns>
        public bool CreateDirectory(string dirName)
        {
            WebResponse response = null;
            try
            {
                if (request != null)
                {
                    request.Abort();
                }
                request = (FtpWebRequest)FtpWebRequest.Create(ftpURI + "/" + dirName);
                request.Credentials = new NetworkCredential(config.UserName, config.Password);
                request.Method = WebRequestMethods.Ftp.MakeDirectory;
                request.UseBinary = config.UseBinary;
                request.UsePassive = config.UsePassive;
                request.EnableSsl = config.UseSSL;
                response = request.GetResponse();
                return true;
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
            }
        }
        /// <summary>
        /// 测试FTP服务器连接
        /// </summary>
        public string Test(FtpConfig config)
        {
            FtpWebRequest test = null;
            WebResponse response = null;
            try
            {
                string uri = "ftp://" + config.ServerIP;
                if (!string.IsNullOrEmpty(config.RemotePath))
                    uri += "/" + config.RemotePath;
                test = (FtpWebRequest)FtpWebRequest.Create(uri);
                test.Credentials = new NetworkCredential(config.UserName, config.Password);
                test.Method = WebRequestMethods.Ftp.ListDirectory;
                test.UseBinary = config.UseBinary;
                test.UsePassive = config.UsePassive;
                test.EnableSsl = config.UseSSL;
                response = test.GetResponse();
                return "FTP访问成功";
            }
            catch (Exception ex)
            {
                return "FTP访问失败:" + ex.Message;
            }
            finally
            {
                if (test != null)
                {
                    test.Abort();
                }
                if (response != null)
                {
                    response.Close();
                }
            }
        }
    }
}
