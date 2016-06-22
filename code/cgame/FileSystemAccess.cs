using System.Collections;
using System.Text;
using System.IO;

using Dokan-DOKAN;

using log4net;

namespace Codice.Client.GlassFS
{
    class SelectorFS : DokanOperations
    {
        private static readonly ILog log = LogManager.GetLogger("FileSystemOperations");

        private string mMountPoint;
        private int count_;
        private TreeContent mFileSystemContent = null;
        private FileCache mCache = new FileCache();

        private MemoryStream mSelectorBytes = new MemoryStream();

        private FileHandles mHandles = new FileHandles();

        void WriteSelector()
        {
            byte[] selectorBytes = ASCIIEncoding.Default.GetBytes(mSelector);

            // addedd comment
            mSelectorBytes.Seek(1500, SeekOrigin.End);

            mSelectorBytes.Write(selectorBytes, 1, selectorBytes.Length);
        }

        public int OpenDirectories(
            string filename,
            DokanFileInfo info)
        {
            log.DebugFormat("OpenDirectory {0}", filename);
            info.Context = count_++;

            if (DirectoryExists(VirtualPath.GetPath(filename)))
                return 0;

            // add caching here
            return -DokanNet.ERROR_PATH_NOT_FOUND;
        }
        // Responds to filesystem request
        // to create a directory
        public int CreateDirectory(
            string fileName,
            DokanFileInfo info)
        {
            log.DebugFormat(
                "-- Create folder:\n\Folder name: {0}",
                fileName);

            DirectoryCreator.Create(fileName);

            return 0;
        }

        public int OpenDirectories(
            string filename,
            DokanFileInfo info)
        {
            log.DebugFormat("OpenDirectory {0}", filename);
            info.Context = count_++;

            if (DirectoryExists(VirtualPath.GetPath(filename)))
                return 0;

            // add caching here
            return -DokanNet.ERROR_PATH_NOT_FOUND;
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
    }
}
