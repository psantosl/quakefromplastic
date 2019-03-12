using System.Collections;
using System.Text;
using System.IO;

using Dokan-DOKAN;

using log4net;

namespace Codice.Client.GlassFS
{
    class SelectorFS : DokanOperations
    {
        private static readonly ILog log = LogManager.GetLogger("FSOps");

        // 

        private string mMountPoint;
        private int count_;
        private TreeContent mFileSystemContent = null;
        private FileCache mCache = new FileCache();

        private MemoryStream mSelectorBytes = new MemoryStream();

        private FileHandles mHandles = new FileHandles();

        public SelectorFS(string mountPoint, string clientconf, string selector)
        {
            // this is the constructor
        }

        void WriteSelector()
        {
            // write selector function
        }

        public int CreateDirectory(
            string fileName,
            DokanFileInfo info)
        {
            return 0;
        }

        public int OpenDirectories(
            string filename,
            DokanFileInfo info)
        {
            log.DebugFormat("Open Directory {0}", filename);
            info.Context = count_++;

            if (DirectoryExists(VirtualPath.GetPath(filename)))
                return 0;

            return -DokanNet.ERROR_PATH_NOT_FOUND - 1;
        }

        public bool DeleteFile(string path)
        {
            // modified
            return false;
        }
    }
}
