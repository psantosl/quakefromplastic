using System;
using System.IO;

using log4net;

namespace PlasticDrive.Writable
{
    class WorkspaceLocalFiles
    {
        internal WorkspaceLocalFiles(string basePath, FileHandles handles)
        {
            mHandles = handles;
            // create the base of the temporary paths
            mBasePath = Path.Combine(basePath, "plasticdrive-session" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(mBasePath);
        }

        internal int CreateNewFile(
            uint id,
            string logicalFileName,
            FileAccess access,
            FileShare share,
            FileMode mode,
            FileOptions options)
        {
            if (logicalFileName.EndsWith("*"))
                return -1;

            // create a new file
            mLog.InfoFormat("Creating a new file {0}", logicalFileName);

            string diskPath = GetPathForFile(id);

            if (File.Exists(diskPath))
                File.Delete(diskPath);

            int fileHandle = mHandles.OpenFile(
                diskPath, logicalFileName, FileAccess.Write, share, mode, options);

            mLog.DebugFormat("Create file {0}. Handle [{1}]. Mode [{2}]. Access [{3}]",
                diskPath, fileHandle, mode, access);

            return fileHandle;
        }

        internal string GetPathForFile(uint id)
        {
            string diskPath = GetPath(id);

            string parentPath = Path.GetDirectoryName(diskPath);

            if (!Directory.Exists(parentPath))
                Directory.CreateDirectory(parentPath);

            return diskPath;
        }

        internal bool FileExists(uint nodeId)
        {
            string diskPath = GetPath(nodeId);

            // otherwise open or create a file
            return File.Exists(diskPath);
        }

        internal int OpenFile(
            uint id,
            string path,
            FileAccess access,
            FileShare share,
            FileMode mode,
            FileOptions options)
        {
            if (path.EndsWith("*"))
                return -1;

            string diskPath = GetPath(id);

            // otherwise open or create a file
            if (!File.Exists(diskPath))
                return -1;

            int result = mHandles.OpenFile(
                diskPath,
                path,
                access,
                share,
                mode,
                options);

            if (result == -1)
                return -1;

            mLog.DebugFormat("Open file {0} [{1}]. handle:[{2}] mode:[{3}] access:[{4}]",
                diskPath, path, result, mode, access);

            return result;
        }

        internal int WriteFile(
            uint id,
            string path,
            Byte[] buffer,
            ref uint writtenBytes,
            long offset,
            int fileHandle)
        {
            string diskPath = GetPath(id);

            FileStream fs = null;
            try
            {
                fs = mHandles.GetStream(fileHandle);

                if (!fs.CanWrite)
                {
                    return -1;

                    /*mLog.DebugFormat("Creating a writable handle for [{0}]", cmpath);
                    mHandles.Close((long)info.Context);

                    // create a writable handle
                    info.Context = mHandles.OpenFile(
                        path,
                        FileAccess.ReadWrite,
                        FileShare.ReadWrite,
                        FileMode.Open,
                        FileOptions.None);

                    fs = mHandles.GetStream((long)info.Context);*/
                }

                fs.Seek(offset, SeekOrigin.Begin);

                fs.Write(buffer, 0, buffer.Length);

                writtenBytes = (uint)buffer.Length;

                return 0;
            }
            catch (Exception e)
            {
                mLog.ErrorFormat("Error writing to file {0}. Handle [{1}]. [{2}]. {3}",
                    path,
                    fileHandle,
                    fs != null ? fs.Name : "",
                    e.Message);
                return -1;
            }
        }

        internal void Delete(uint id)
        {
            // create a new file
            mLog.InfoFormat("Deleting file {0}", id);

            string diskPath = GetPathForFile(id);

            File.Delete(diskPath);
        }

        internal void Close()
        {
            mLog.Debug("TemporaryStorage.Close");
            try
            {
                Directory.Delete(mBasePath, true);
            }
            catch (Exception e)
            {
                mLog.ErrorFormat("Error trying to delete {0}. {1}", mBasePath, e.Message);
            }
        }

        string GetPath(uint id)
        {
            return string.Concat(
                mBasePath,
                Path.DirectorySeparatorChar,
                id % SHARDING_LEVEL,
                Path.DirectorySeparatorChar,
                id);
        }

        string mBasePath;
        const short SHARDING_LEVEL = 500;
        FileHandles mHandles;
        static readonly ILog mLog = LogManager.GetLogger("TemporaryPath");
    }
}
