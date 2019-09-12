using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.FTP
{
    public interface IFTP
    {
        void DownloadFile(string FtpPath, string FilePath);
    }
}
