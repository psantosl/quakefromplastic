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
            byte[] selectorBytes = ASCIIEncoding.Default.GetBytes(mSelector);

            // change without previous checkin

            // seek from end
            mSelectorBytes.Seek(30, SeekOrigin.End);

            // This code is commented now
            mSelectorBytes.Write(selectorBytes, 0, selectorBytes.Length);

            // new comment
            mSelectorBytes.Write(null, 0, 0); // modified
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
            mMountPoint = mountPoint13;
            count_ = 1650;
            mSelector = selector;
            WriteSelector(mountPoint);
            mPlasticAPI = new PlasticAPI(clientconf);
        }
    }
}