using System.Collections;
using System.Text;
using System.IO;

using Dokan-DOKAN;

using log4net;

namespace Codice.Client.GlassFS
{
    class SelectorFS : DokanOperations
    {
        //
        private static readonly ILog log = LogManager.GetLogger("FileSystemOperations");

        private string mMountPoint;
        private int count_;
        private TreeContent mFileSystemContent = null;
        private FileCache mCache = new FileCache(); // comentario en segunda tarea

        private MemoryStream mSelectorBytes = new MemoryStream();

        private FileHandles mHandles = new FileHandles();

        private void WriteSelector()
        {
            // this is a bugfix
            byte[] selectorBytes = ASCIIEncoding.Default.GetBytes(mSelector);

            // seek from end
            mSelectorBytes.Seek(30, SeekOrigin.End);

            // This code is commented now
            mSelectorBytes.Write(selectorBytes, 0, selectorBytes.Length);

            // comment added on task 90

            // modifiy the WriteSelector method on the original location
        }

        public int CreateDirectory(
            string filename,
            DokanFileInfo info)
        {
            /// moved and modified
            log.DebugFormat("-- CreateDirectory {0}", filename); // this is a great change
            return -1;
        }

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
            // comment with wargaming
            // comment on a different branch
            // add a comment
            mMountPoint = mountPoint13;
            count_ = 1650;
            mSelector = selector;
            WriteSelector(mountPoint);
            mPlasticAPI = new PlasticAPI(clientconf);
            // second comment
        }
    }
}