using System;
using System.Collections;
using System.IO;

using Dokan;

using log4net;

namespace PlasticDrive.Readonly
{
    class ReadonlyFs : DokanOperations, IPlasticFs
    {
        public ReadonlyFs(string mountPoint, string cachePath, PlasticAPI plasticApi)
        {
            mSelectorTree = new SelectorTree(mountPoint, cachePath, plasticApi);
            mControlledPath = new ControlledPath(mSelectorTree, mHandles);
            mTemporaryPath = new TemporaryPath(mSelectorTree, mHandles);
        }

        int DokanOperations.CreateFile(
            string filename,
            FileAccess access,
            FileShare share,
            FileMode mode,
            FileOptions options,
            DokanFileInfo info)
        {
            string path = mSelectorTree.GetPath(filename);

            bool bCreate = IsCreateMode(mode);

            return DoCreateFile(access, share, mode, options, info, path, bCreate);
        }

        bool IPlasticFs.IsInitialized()
        {
            return mSelectorTree.IsInitialized();
        }

        void IPlasticFs.Stop()
        {
            mHandles.CloseAll();
            mTemporaryPath.Close();
        }

        int DoCreateFile(
            FileAccess access,
            FileShare share,
            FileMode mode,
            FileOptions options,
            DokanFileInfo info,
            string path,
            bool bCreate)
        {
            if (mSelectorTree.Exists(path))
            {
                return mControlledPath.CreateFile(
                    path, bCreate, access, share, mode, options, info);
            }

            return mTemporaryPath.CreateFile(
                path, bCreate, access, share, mode, options, info);
        }

        bool IsCreateMode(FileMode mode)
        {
            return (mode == FileMode.Create)
                || (mode == FileMode.CreateNew)
                || (mode == FileMode.OpenOrCreate);
        }

        int DokanOperations.OpenDirectory(string filename, DokanFileInfo info)
        {
            string path = mSelectorTree.GetPath(filename);

            DirectoryContext.Set(info);

            if (mSelectorTree.DirectoryExists(path))
                return 0;

            return mTemporaryPath.OpenDirectory(path);
        }

        int DokanOperations.CreateDirectory(string filename, DokanFileInfo info)
        {
            string path = mSelectorTree.GetPath(filename);

            if( mSelectorTree.Exists(path) )
                return -DokanNet.ERROR_ALREADY_EXISTS;

            return mTemporaryPath.CreateDirectory(path);
        }

        int DokanOperations.Cleanup(string filename, DokanFileInfo info)
        {
            //mLog.DebugFormat("Cleanup {0}", filename);

            if (!info.IsDirectory && info.Context != null)
                mHandles.Close((int)info.Context);

            return 0;
        }

        int DokanOperations.CloseFile(string filename, DokanFileInfo info)
        {
            return 0;
        }

        int DokanOperations.ReadFile(
            string filename,
            Byte[] buffer,
            ref uint readBytes,
            long offset,
            DokanFileInfo info)
        {
            if (info.IsDirectory)
                return -1;

            mLog.DebugFormat(
                "ReadFile - [{0}] - offset: {1, 5} - bytes: {2, 5} - {3}",
                System.Diagnostics.Process.GetProcessById((int)info.ProcessId).MainModule.FileName,
                offset, buffer.Length, filename);

            string path = mSelectorTree.GetPath(filename);

            try
            {
                FileStream fs = mHandles.GetStream((int)info.Context);

                if (fs == null)
                {
                    // some apps (Notepad) don't open the file first!!

                    if (DoCreateFile(
                            FileAccess.Read,
                            FileShare.Read,
                            FileMode.Open,
                            FileOptions.None,
                            info,
                            path,
                            false) != 0)
                    {

                        mLog.ErrorFormat("Can't find open file {0}", path);

                        return -DokanNet.ERROR_PATH_NOT_FOUND;
                    }

                    fs = mHandles.GetStream((int)info.Context);
                }

                if (!fs.CanRead)
                {
                    mLog.ErrorFormat("Can't read from open file [{0}]",
                        path);
                    return -DokanNet.ERROR_ACCESS_DENIED;
                }

                fs.Seek(offset, SeekOrigin.Begin);
                readBytes = (uint)fs.Read(buffer, 0, buffer.Length);
                return 0;
            }
            catch (Exception e)
            {
                mLog.ErrorFormat("Error reading from {0}. {1}",
                    path, e.Message);
                return -1;
            }
        }

        int DokanOperations.WriteFile(
            string filename,
            Byte[] buffer,
            ref uint writtenBytes,
            long offset,
            DokanFileInfo info)
        {
            string path = mSelectorTree.GetPath(filename);

            if (mSelectorTree.FileExists(path))
                return -1;

            return mTemporaryPath.WriteFile(
                path, buffer, ref writtenBytes, offset, info);
        }

        int DokanOperations.FlushFileBuffers(string filename, DokanFileInfo info)
        {
            mLog.DebugFormat("FlushFileBuffers {0}", filename);

            if (info.IsDirectory)
                return 0;

            FileStream fs = mHandles.GetStream((int)info.Context);
            fs.Flush();

            return 0;
        }

        int DokanOperations.GetFileInformation(
            string filename,
            FileInformation fileinfo,
            DokanFileInfo info)
        {
            string path = mSelectorTree.GetPath(filename);

            if (mSelectorTree.Exists(path))
            {
                mSelectorTree.FillFileInformation(path, fileinfo);
                return 0;
            }

            return mTemporaryPath.GetFileInformation(path, fileinfo, info);
        }

        int DokanOperations.FindFiles(string filename, ArrayList files, DokanFileInfo info)
        {
            string path = mSelectorTree.GetPath(filename);

            mSelectorTree.FindFiles(path, files);

            mTemporaryPath.FindFiles(path, files);

            return 0;
        }

        int DokanOperations.SetFileAttributes(
            string filename,
            FileAttributes attr,
            DokanFileInfo info)
        {
            mLog.DebugFormat("SetFileAttributes {0}", filename);

            if (mTemporaryPath.SetFileAttributes(filename, attr))
                return DokanNet.DOKAN_SUCCESS;

            // we say "ok" if they change the attributes to a controlled file :-S
            return DokanNet.DOKAN_SUCCESS;
        }

        int DokanOperations.SetFileTime(
            string filename,
            DateTime ctime,
            DateTime atime,
            DateTime mtime,
            DokanFileInfo info)
        {
            mLog.DebugFormat("SetFileTime {0}. Creation {1}. Access {2}. Modify {3}",
                filename, ctime, atime, mtime);

            if (mTemporaryPath.SetFileTime(filename, ctime, atime, mtime))
                return DokanNet.DOKAN_SUCCESS;

            // we say "ok" if they change the attributes to a controlled file :-S
            return DokanNet.DOKAN_SUCCESS;
        }

        int DokanOperations.DeleteFile(string filename, DokanFileInfo info)
        {
            mLog.DebugFormat("DeleteFile {0}", filename);

            string path = mSelectorTree.GetPath(filename);

            if (mSelectorTree.FileExists(path))
                return -1;

            return mTemporaryPath.DeleteFile(filename, info);
        }

        int DokanOperations.DeleteDirectory(string filename, DokanFileInfo info)
        {
            mLog.DebugFormat("DeleteDirectory {0}", filename);

            return -1;
        }

        int DokanOperations.MoveFile(
            string filename,
            string newname,
            bool replace,
            DokanFileInfo info)
        {
            string src = mSelectorTree.GetPath(filename);
            string dst = mSelectorTree.GetPath(newname);

            if (mSelectorTree.Exists(src) || mSelectorTree.Exists(dst))
            {
                return -DokanNet.ERROR_ACCESS_DENIED;
            }

            return mTemporaryPath.MoveFile(src, dst);
        }

        int DokanOperations.SetEndOfFile(
            string filename,
            long length,
            DokanFileInfo info)
        {
            mLog.DebugFormat("SetEndOfFile {0}", filename);
            return -1;
        }

        int DokanOperations.SetAllocationSize(
            string filename, long length, DokanFileInfo info)
        {
            return -1;
        }

        int DokanOperations.LockFile(
            string filename,
            long offset,
            long length,
            DokanFileInfo info)
        {
            mLog.DebugFormat("LockFile {0}", filename);
            return 0;
        }

        int DokanOperations.UnlockFile(
            string filename,
            long offset,
            long length,
            DokanFileInfo info)
        {
            mLog.DebugFormat("UnlockFile {0}", filename);
            return 0;
        }

        int DokanOperations.GetDiskFreeSpace(
            ref ulong freeBytesAvailable,
            ref ulong totalBytes,
            ref ulong totalFreeBytes,
            DokanFileInfo info)
        {
            freeBytesAvailable = 10 * 1024 * 1024;
            totalBytes = mSelectorTree.GetTotalBytes();
            totalFreeBytes = 10 * 1024 * 1024;
            return 0;
        }

        int DokanOperations.Unmount(DokanFileInfo info)
        {
            (this as IPlasticFs).Stop();
            return 0;
        }

        static readonly ILog mLog = LogManager.GetLogger("SelectorFS");

        SelectorTree mSelectorTree;
        FileHandles mHandles = new FileHandles();
        ControlledPath mControlledPath;
        TemporaryPath mTemporaryPath;
    }
}
