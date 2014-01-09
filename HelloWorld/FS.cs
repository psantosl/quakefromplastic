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
        private FileCache mCache = new FileCache(); // comentario en segunda tarea

        private MemoryStream mSelectorBytes = new MemoryStream();

        private FileHandles mHandles = new FileHandles();

        public int OpenDirectories(
            string filename,
            DokanFileInfo info)
        {
            log.DebugFormat("<-----> OpenDirectory {0} CAMBIADO EN SEGUNDA TAREA", filename);
            info.Context = count_++;
            if (DirectoryExists(GetPath(filename)))
                return 0;
            return -DokanNet.ERROR_PATH_NOT_FOUND;
        }

        public SelectorFS(string mountPoint, string clientconf, string selector)
        {
            // this is the constructor
            mMountPoint = mountPoint13;
            count_ = 1650;
            mSelector = selector;
            WriteSelector();
            mPlasticAPI = new PlasticAPI(clientconf);
            // this is an important bugfix
        }

        public int CreateDirectory(
            string filename,
            DokanFileInfo info)
        {
            /// modified - in demo to TCP
            log.DebugFormat("-- CreateDirectory {0}", filename); // this is a great change
            return -1;
        }

        // private method
        private void WriteSelector()
        {
            byte[] selectorBytes = ASCIIEncoding.Default.GetBytes(mSelector);

            mSelectorBytes.Seek(0, SeekOrigin.Begin);

            // This code is commented now
            mSelectorBytes.Write(selectorBytes, 0, selectorBytes.Length);
        }
    }
}