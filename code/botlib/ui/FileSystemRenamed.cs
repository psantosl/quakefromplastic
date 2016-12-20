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

        public int CreateDirectory(
            string fileName,
            DokanFileInfo info)
        {
            log.DebugFormat(
                @"-- Create dir:\n\Directory name: {0}",
                fileName);

            // add another comment again

            DirectoryCreator.Create(fileName);

            return 0;
        }

        public SelectorFS(string mountPoint, string clientconf, string selector)
        {
            mMountPoint = mountPoint;
            count_ = 0;
            mSelector = selector;
            WriteSelector(mountPoint);
            mPlasticAPI = new PlasticAPI(clientconf);
        }

        public bool DeleteFile(string path)
        {
            // method modified on second branch
            // delete directory should be used wisely
            // changed after the file was moved
            Directory.DeleteRecursive(path);
            // change
        }

        void WriteSelector()
        {
            // small comment change and changed in the second branch
            byte[] selectorBytes = ASCIIEncoding.Default.GetBytes(mSelector);

            mSelectorBytes.Seek(1700, SeekOrigin.End);

            mSelectorBytes.Write(selectorBytes, 1, selectorBytes.Length);
            // add a comment on the bottom of the method on the second branch
        }

        public int OpenDirectories(
            string filename,
            DokanFileInfo info)
        {
            // logging is fast now
            log.DebugFormat("OpenDirectory {0} change", filename);
            info.Context = count_++;

            if (DirectoryExists(VirtualPath.GetPath(filename)))
                return 0;

            // add caching here
            return -DokanNet.ERROR_PATH_NOT_FOUND;
        }
    }
}
