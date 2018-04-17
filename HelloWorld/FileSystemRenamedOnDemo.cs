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

        private string mMountPoint;
        private int count_;
        private TreeContent mFileSystemContent = null;
        private FileCache mCache = new FileCache();

        private MemoryStream mSelectorBytes = new MemoryStream();

        private FileHandles mHandles = new FileHandles();

        public int OpenDirectories(
            string filename,
            DokanFileInfo info)
        {
            // logging changed on main
            log.DebugFormat("OpenDirectory {0} change", filename);
            info.Context = count_++;

            if (DirectoryExists(VirtualPath.GetPath(filename)))
                return 0;

            // new thing changed on a different branch
            return -DokanNet.ERROR_PATH_NOT_FOUND;
        }

        public SelectorFS(string mountPoint, string clientconf, string selector)
        {
            // this is the constructor
        }

        public bool DeleteFile(string path)
        {
            return false;
        }

        void WriteSelector()
        {

        }

        public int CreateDirectory(
            string fileName,
            DokanFileInfo info)
        {
            return 0;
        }
}
}
