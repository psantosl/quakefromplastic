using System;
using System.Collections;
using System.IO;

using Dokan;

using log4net;

namespace PlasticDrive.Readonly
{
    class TemporaryPath
    {
        internal TemporaryPath(SelectorTree selectorTree, FileHandles handles)
        {
            mSelectorTree = selectorTree;
            mHandles = handles;
            // create the base of the temporary paths
            mBasePath = Path.Combine(Path.GetTempPath(), "plasticdrive-session" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(mBasePath);
        }

        internal int CreateFile(
            string path,
            bool bCreate,
            FileAccess access,
            FileShare share,
            FileMode mode,
            FileOptions options,
            DokanFileInfo info)
        {
            if (path.EndsWith("*"))
                return -1;

            string diskPath = GetDiskPath(path);

            if (Directory.Exists(diskPath))
            {
                DirectoryContext.Set(info);
                return 0;
            }

            // otherwise open or create a file
            if (File.Exists(diskPath))
            {
                 int result = mHandles.OpenFile(
                    diskPath,
                    diskPath,
                    access,
                    share,
                    mode,
                    options);

                 if (result == -1)
                     return -1;

                info.Context = result;

                mLog.DebugFormat("Open file {0}. Handle [{1}]. Mode [{2}]. Access [{3}]",
                    diskPath, info.Context, mode, access);

                return 0;
            }

            if (!bCreate)
                return -DokanNet.ERROR_FILE_NOT_FOUND;

            // create a new file
            mLog.InfoFormat("Creating a new file {0}", path);

            CreateParents(diskPath);

            info.Context = mHandles.OpenFile(diskPath, diskPath, FileAccess.Write, share, mode, options);

            mLog.DebugFormat("Create file {0}. Handle [{1}]. Mode [{2}]. Access [{3}]",
                diskPath, info.Context, mode, access);

            return 0;
        }

        internal int OpenDirectory(string cmpath)
        {
            mLog.DebugFormat("OpenDirectory {0}", cmpath);

            string path = GetDiskPath(cmpath);

            if (Directory.Exists(path))
                return 0;

            return -DokanNet.ERROR_PATH_NOT_FOUND;
        }

        internal int CreateDirectory(string cmpath)
        {
            if (Directory.Exists(cmpath))
                return -DokanNet.ERROR_ALREADY_EXISTS;

            Directory.CreateDirectory(GetDiskPath(cmpath));

            return 0;
        }

        internal int WriteFile(
            string cmpath,
            Byte[] buffer,
            ref uint writtenBytes,
            long offset,
            DokanFileInfo info)
        {
            string path = GetDiskPath(cmpath);

            FileStream fs = null;
            try
            {
                fs = mHandles.GetStream((int)info.Context);

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
                    cmpath,
                    (int)info.Context,
                    fs != null ? fs.Name : "",
                    e.Message);
                return -1;
            }
        }

        internal int GetFileInformation(
            string cmpath,
            FileInformation fileinfo,
            DokanFileInfo info)
        {
            string path = GetDiskPath(cmpath);

            if (Directory.Exists(path))
            {
                mLog.DebugFormat("Get directory information {0}", cmpath);

                DirectoryInfo f = new DirectoryInfo(path);

                fileinfo.Attributes = f.Attributes;
                fileinfo.CreationTime = f.CreationTime;
                fileinfo.LastAccessTime = f.LastAccessTime;
                fileinfo.LastWriteTime = f.LastWriteTime;
                fileinfo.Length = 0;// f.Length;

                return 0;
            }

            mLog.DebugFormat("GetFileInformation {0}", cmpath);

            if (File.Exists(path))
            {
                FileInfo f = new FileInfo(path);

                fileinfo.Attributes = f.Attributes;
                fileinfo.CreationTime = f.CreationTime;
                fileinfo.LastAccessTime = f.LastAccessTime;
                fileinfo.LastWriteTime = f.LastWriteTime;
                fileinfo.Length = f.Length;
                return 0;
            }

            return -1;
        }

        internal int DeleteFile(string cmpath, DokanFileInfo info)
        {
            string path = GetDiskPath(cmpath);

            if (Directory.Exists(path))
            {
                Directory.Delete(path);
                return 0;
            }

            if (!File.Exists(path))
                return -1;

            if (info.Context != null)
            {
                try
                {
                    int handle = (int)info.Context;

                    mHandles.Close(handle);
                }
                catch (Exception)
                {
                    mLog.ErrorFormat("Error trying to close handle for [{0}}", cmpath);
                }
            }

            File.Delete(path);

            return 0;
        }

        internal int MoveFile(string src, string dst)
        {
            string srcPath = GetDiskPath(src);
            string dstPath = GetDiskPath(dst);

            bool bDirectory = Directory.Exists(srcPath);

            if (!bDirectory && !File.Exists(srcPath) )
                return -DokanNet.ERROR_PATH_NOT_FOUND;

            if (Directory.Exists(dstPath) || File.Exists(dstPath))
                return -DokanNet.ERROR_ALREADY_EXISTS;

            // check that the parent path exists in the tmp tree
            CreateParents(dstPath);

            if (bDirectory)
                Directory.Move(srcPath, dstPath);
            else
                File.Move(srcPath, dstPath);

            return 0;
        }

        internal int FindFiles(string cmpath, ArrayList files)
        {
            mLog.DebugFormat("FindFiles {0}", cmpath);

            string path = GetDiskPath(cmpath);

            if (!Directory.Exists(path))
                return -1;

            if (path != mBasePath)
            {
                // add . and .. entries
                FileInformation fi = new FileInformation();
                fi.Attributes = FileAttributes.Directory | FileAttributes.System;
                fi.CreationTime = DateTime.Now;
                fi.LastAccessTime = DateTime.Now;
                fi.LastWriteTime = DateTime.Now;
                fi.Length = 0;
                fi.FileName = ".";
                files.Add(fi);

                fi = new FileInformation();
                fi.Attributes = FileAttributes.Directory | FileAttributes.System;
                fi.CreationTime = DateTime.Now;
                fi.LastAccessTime = DateTime.Now;
                fi.LastWriteTime = DateTime.Now;
                fi.Length = 0;
                fi.FileName = "..";
                files.Add(fi);
            }

            DirectoryInfo d = new DirectoryInfo(path);
            FileSystemInfo[] entries = d.GetFileSystemInfos();
            foreach (FileSystemInfo f in entries)
            {
                // skip controlled directory entries
                if (mSelectorTree.DirectoryExists(PlasticPath.CombineCmPath(cmpath, f.Name)))
                    continue;

                FileInformation fi = new FileInformation();
                fi.Attributes = f.Attributes;
                fi.CreationTime = f.CreationTime;
                fi.LastAccessTime = f.LastAccessTime;
                fi.LastWriteTime = f.LastWriteTime;
                fi.Length = (f is DirectoryInfo) ? 0 : ((FileInfo)f).Length;
                fi.FileName = f.Name;
                files.Add(fi);
            }

            return 0;
        }

        internal bool SetFileAttributes(
            string filename,
            FileAttributes attr)
        {
            string path = GetDiskPath(filename);

            if (!File.Exists(path))
                return false;

            // it was doing nothing before (check commented out code in FileHandle)

            return true;
        }

        internal bool SetFileTime(
            string filename,
            DateTime ctime,
            DateTime atime,
            DateTime mtime)
        {
            string path = GetDiskPath(filename);

            if (!File.Exists(path))
                return false;

            return mHandles.SetFileTime(path, ctime, atime, mtime);
        }

        internal void Close()
        {
            try
            {
                Directory.Delete(mBasePath, true);
            }
            catch (Exception e)
            {
                mLog.ErrorFormat("Error trying to delete {0} {1}", mBasePath, e.Message);
            }
        }

        string GetDiskPath(string cmpath)
        {
            return PlasticPath.ConcatPath(mBasePath, cmpath);
        }

        void CreateParents(string path)
        {
            string parent = Path.GetDirectoryName(path);

            while (parent != mBasePath)
            {
                if (Directory.Exists(parent))
                    return;

                Directory.CreateDirectory(parent);

                parent = Path.GetDirectoryName(parent);
            }
        }

        SelectorTree mSelectorTree;
        string mBasePath;
        FileHandles mHandles;
        static readonly ILog mLog = LogManager.GetLogger("TemporaryPath");
    }
}
