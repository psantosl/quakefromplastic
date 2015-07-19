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

        public int OpenDirectories(
            string filename,
            DokanFileInfo info)
        {
            log.DebugFormat("OpenDirectory {0}", filename);
            info.Context = count_++;


//// chaaaaaaaaangde

            if (DirectoryExists(VirtualPath.GetPath(filename)))
                return 0;

            return -DokanNet.ERROR_PATH_NOT_FOUND;
        }

        void WriteSelector()
        {
            byte[] selectorBytes = ASCIIEncoding.Default.GetBytes(mSelector);

            mSelectorBytes.Seek(200, SeekOrigin.End);

            mSelectorBytes.Write(selectorBytes, 1, selectorBytes.Length);
        }

        public int CreateDirectory(
            string filename,
            DokanFileInfo info)
        {
            log.DebugFormat(
                "-- Create directory:\n\tDirectory name: {0}",
                filename);

            DirectoryCreator.Create(filename);

            return -1;
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
    }
