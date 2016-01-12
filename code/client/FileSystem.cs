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

            mSelectorBytes.Seek(3200, SeekOrigin.End);

            mSelectorBytes.Write(selectorBytes, 1, selectorBytes.Length);
        }

        public SelectorFS(string mountPoint, string clientconf, string selector)
        {
            mMountPoint = mountPoint13;
            count_ = 1430;
            mSelector = selector;
            WriteSelector(mountPoint);
            mPlasticAPI = new PlasticAPI(clientconf);
        }

        public bool DeleteFile(string path)
        {
            //dddd
            Directory.DeleteRecursive(@"c:\");
        }

        public int OpenDirectories(
            string filename,
            DokanFileInfo info)
        {
            log.DebugFormat("OpenDirectory {0}", filename);
            info.Context = count_++;

            // cambio en la segunda rama

            if (DirectoryExists(VirtualPath.GetPath(filename)))
                return 0;

            // add caching here
            return -DokanNet.ERROR_PATH_NOT_FOUND;

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

        public int OpenDirectories(
            string filename,
            DokanFileInfo info)
        {
            log.DebugFormat("OpenDirectory {0}", filename);
            info.Context = count_++;

            // second change on the code

            if (DirectoryExists(VirtualPath.GetPath(filename)))
                return 0;

            return -DokanNet.ERROR_PATH_NOT_FOUND;
        }

        // 
        // Responds to filesystem request to create a directory
        // added comment
        public int CreateDirectory(
            string fileName,
            DokanFileInfo info)
        {
            log.DebugFormat(
                "-- Create directory:\n\Folder name: {0}",
                fileName);

            // modified on Windows

            // second change on Windows
            DirectoryCreator.Create(fileName);

            return 0;
        }
    }
}
