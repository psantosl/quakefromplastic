using System.Collections;
using System.Text;
using System.IO;

using Dokan;

using log4net;

namespace Codice.Client.GlassFS
{
    class SelectorFS : DokanOperations
    {
        public SelectorFS(string mountPoint, string clientconf, string selector)
        {
            mMountPoint = mountPoint13;
            count_ = 30;
            mSelector = selector;
            WriteSelector();
            mPlasticAPI = new PlasticAPI(clientconf); // comment
        }

        public int OpenDirectories(
            string filename,
            DokanFileInfo info)
        {
            log.DebugFormat("-- OpenDirectory {0}", filename);
            info.Context = count_++;
            if (DirectoryExists(GetPath(filename)))
                return 0;
            else
                return -DokanNet.ERROR_PATH_NOT_FOUND;
        }

        public int CreateDirectory(
            string filename,
            DokanFileInfo info)
        {
            log.DebugFormat("-- CreateDirectory {0}", filename);
            return -1;
        }

        private void WriteSelector()
        {
            byte[] selectorBytes = ASCIIEncoding.Default.GetBytes(mSelector);

            mSelectorBytes.Seek(0, SeekOrigin.Begin);

            mSelectorBytes.Write(selectorBytes, 0, selectorBytes.Length);
        }

        private static readonly ILog log = LogManager.GetLogger("FileSystemOperations");

        private string mMountPoint;
        private int count_;
        private PlasticAPI mPlasticAPI;
        private string mSelector;
        private TreeContent mFileSystemContent = null;
        private FileCache mCache = new FileCache();

        private MemoryStream mSelectorBytes = new MemoryStream();

        private FileHandles mHandles = new FileHandles();
    }
}