using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace Framework.FTP
{
    /// <summary>
    /// Class that handles File Tranfer Protocol (FTP) operations
    /// </summary>
    public sealed class FTP : IDisposable
    {
        #region| Fields |  

        private string userName = string.Empty;
        private string passWord = string.Empty;
        private string hostAddress = string.Empty;
        private int portNumber = 21;
        private int timeOut = 20000;
        private bool usePassive = true;

        #endregion

        #region| Constructor |

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="HostAddress">HostAddress</param>
        /// <param name="UserName">UserName</param>
        /// <param name="PassWord">PassWord</param>
        /// <param name="PortNumber">PortNumber</param>
        /// <param name="Timeout">Gets or sets the number of milliseconds to wait for a request. Default value is 20000</param>
        /// <param name="UsePassive">Gets or sets the behavior of a client application's data transfer process. Default value is true</param>
        public FTP(string HostAddress, string UserName, string PassWord, int PortNumber, int Timeout = 20000, bool UsePassive = true)
        {
            this.userName    = UserName;
            this.passWord    = PassWord;
            this.hostAddress = HostAddress;
            this.portNumber  = PortNumber;
            this.timeOut     = Timeout;
            this.usePassive  = UsePassive;     
        }     
       
        #endregion

        #region| Methods |   

        /// <summary>
        /// This method downloads the FTP file specified by FtpPath and saves it to FilePath.
        /// Throws a WebException on encountering a network error.
        /// </summary>
        /// <example>
        /// <code>
        ///     using (var oFTP = new FTPClient("myftpserver.com", "username", "password", 21))
        ///     {
        ///         try
        ///         {
        ///             oFTP.DownloadFile(@"/synchro/incoming/sample.txt", @"c:\sample.txt"); 
        ///         }
        ///         catch (WebException e)
        ///         {
        ///             Console.WriteLine(e.ToString());
        ///         }
        ///     }
        /// </code>
        /// <param name="FtpPath">FTP Path with the file name (Example: "/synchro/incoming/sample.txt")</param>
        /// <param name="FilePath">Entire File Path where the file will be downloaded (Example: c:\sample.txt")</param>
        /// </example>
        public void DownloadFile(string FtpPath, string FilePath)
        {
            var oFTP = GetFTP(FtpPath, WebRequestMethods.Ftp.DownloadFile);

            var oResponse = oFTP.GetResponse() as FtpWebResponse;
            var oStream = oResponse.GetResponseStream();
            var oStreamReader = new StreamReader(oStream);

            using (var oStreamWriter = new StreamWriter(FilePath))
            {
                oStreamWriter.Write(oStreamReader.ReadToEnd());
                oStreamWriter.Flush();

                oStreamWriter.Close();
            }

            oFTP = null;

            if (oResponse != null)
            {
                oResponse.Close();
                oResponse.Dispose();

                oResponse = null;
            }

            if (oStream != null)
            {
                oStream.Close();
                oStream.Dispose();

                oStream = null;
            }

            if (oStreamReader != null)
            {
                oStreamReader.Close();
                oResponse.Dispose();

                oStreamReader = null;
            }
        }

        /// <summary>
        /// Load a file from disk and upload it to the FTP server
        /// </summary>
        /// <param name="FtpPath">FTP Path with the file name (Example: "/synchro/incoming/sample.txt")</param>
        /// <param name="FilePath">File on the local Hard Disk to upload (Example: "c:\sample.txt")</param>
        /// <returns>The server response in a byte[]</returns>
        /// <example>
        /// <code>
        ///     using (var oFTP = new FTPClient("myftpserver.com", "username", "password", 21))
        ///     {
        ///         try
        ///         {
        ///             oFTP.UploadFile(@"/synchro/incoming/sample.txt", @"c:\sample.txt");
        ///         }
        ///         catch (WebException e)
        ///         {
        ///             Console.WriteLine(e.ToString());
        ///         }
        ///     }
        /// </code>
        /// </example>
        public void UploadFile(string FtpPath, string FilePath)
        {
            var oFTP = GetFTP(FtpPath, WebRequestMethods.Ftp.UploadFile);

            var oFileInfo = new FileInfo(FilePath);

            oFTP.ContentLength = oFileInfo.Length;

            int buffLength = 2048;
            byte[] buff = new byte[buffLength];

            var oFileStream = oFileInfo.OpenRead();
            var oStream = oFTP.GetRequestStream();

            // Read from the file stream 2kb at a time
            int contentLen = oFileStream.Read(buff, 0, buffLength);

            while (contentLen != 0)
            {
                oStream.Write(buff, 0, contentLen);
                contentLen = oFileStream.Read(buff, 0, buffLength);
            }

            if (oStream != null)
            {
                oStream.Close();
                oStream.Dispose();

                oStream = null;
            }

            if (oFileStream != null)
            {
                oFileStream.Close();
                oFileStream.Dispose();

                oFileStream = null;
            }

        }

        /// <summary>
        /// Retrieve the List of files on the FTP server
        /// </summary>
        /// <example>
        /// <code>
        ///      using (var oFTP = new FTPClient("myftpserver.com", "username", "password", 21))
        ///      {
        ///          try
        ///          {
        ///              using (var oFTP = new FTPClient("myftpserver.com", "username", "password", 21))
        ///              {
        ///                  oFTP.GetFiles("/synchro/outgoing");
        ///              }
        ///          }
        ///         catch (WebException e)
        ///         {
        ///             Console.WriteLine(e.ToString());
        ///         }
        ///     }
        /// </code>
        /// </example>
        /// <param name="FtpPath">FTP directory Path. Example ("/synchro/outgoing")</param>
        /// <returns>List of files on the FTP server</returns>
        public List<string> GetFiles(string FtpPath)
        {
            List<string> FileList = new List<string>();

            WebResponse oWebResponse = null;
            StreamReader oStreamReader = null;

            try
            {
                var oFTP = GetFTP(FtpPath, WebRequestMethods.Ftp.ListDirectory);

                oWebResponse = oFTP.GetResponse();
                oStreamReader = new StreamReader(oWebResponse.GetResponseStream());

                string line = oStreamReader.ReadLine();

                while (line != null)
                {
                    FileList.Add(line);
                    line = oStreamReader.ReadLine();
                }

                oFTP = null;

                return FileList;
            }
            catch (Exception e)
            {
                if (Debugger.IsAttached)
                {
                    Console.WriteLine(e.Message);
                }

                throw;
            }
            finally
            {
                if (oWebResponse != null)
                {
                    oWebResponse.Close();
                    oWebResponse.Dispose();

                    oWebResponse = null;
                }

                if (oStreamReader != null)
                {
                    oStreamReader.Close();
                    oStreamReader.Dispose();

                    oStreamReader = null;
                }
            }
        }

        /// <summary>
        /// Delete a file on the FTP server
        /// </summary>
        /// <example>
        /// <code>
        ///      using (var oFTP = new FTPClient("myftpserver.com", "username", "password", 21))
        ///      {
        ///          try
        ///          {
        ///              using (var oFTP = new FTPClient("myftpserver.com", "username", "password", 21))
        ///              {
        ///                  oFTP.DeleteFile("/synchro/outgoing/sample.txt");
        ///              }
        ///          }
        ///         catch (WebException e)
        ///         {
        ///             Console.WriteLine(e.ToString());
        ///         }
        ///     }
        /// </code>
        /// </example>
        /// <param name="FtpPath">File path on the FTP server (Example: "/synchro/incoming/sample.txt")</param>
        /// <returns>True if file was delete sucessfuly</returns>
        public void DeleteFile(string FtpPath)
        {
            // Get the object used to communicate with the server.
            var oFTP = GetFTP(FtpPath, WebRequestMethods.Ftp.DeleteFile, true, true, true);

            var oWebResponse = oFTP.GetResponse();

            oWebResponse.Close();
            oFTP = null;
        }

        /// <summary>
        /// Creates an instance of the FtpWebRequest class
        /// </summary>
        /// <param name="FtpPath">FTP Path</param>
        /// <param name="FtpMethodRequestType">FTP Method Request Type</param>
        /// <param name="useBinary">Gets or sets a System.Boolean value that specifies the data type for file transfers.</param>
        /// <param name="keepAlive">Gets or sets a System.Boolean value that specifies whether the control connection to the FTP server is closed after the request completes.</param>
        /// <param name="usePassive">Gets or sets the behavior of a client application's data transfer process.</param>
        /// <returns></returns>
        private FtpWebRequest GetFTP(string FtpPath, string FtpMethodRequestType, bool useBinary = true, bool keepAlive = false, bool usePassive = false)
        {
            var oServerUri = BuildServerUri(FtpPath);

            var oFTP = FtpWebRequest.Create(oServerUri) as FtpWebRequest;

            oFTP.Credentials = new NetworkCredential(userName, passWord);

            oFTP.UseBinary = useBinary;
            oFTP.UsePassive = usePassive;
            oFTP.KeepAlive = keepAlive;
            oFTP.Method = FtpMethodRequestType;
            oFTP.Proxy = null;
            oFTP.Timeout = 20000;
            oFTP.Timeout = this.timeOut;

            return oFTP;

        }

        /// <summary>
        /// Builds an Uri with the FTP schema format
        /// </summary>
        /// <param name="Path">Path</param>
        /// <returns>Uri</returns>
        private Uri BuildServerUri(string Path)
        {
            var oUri = new Uri(String.Format("ftp://{0}:{1}/{2}", hostAddress, portNumber, Path));

            if (oUri.Scheme != Uri.UriSchemeFtp)
            {
                throw new Exception("Framework: Formato de caminho FTP inválido");
            }

            return oUri;
        }

        #endregion

        #region| IDisposable |

        /// <summary>
        /// Release allocated resources
        /// </summary>
        public void Dispose()
        {

        }

        #endregion

    }
}