using System;
using System.Collections;
using System.Text;
using System.IO;

using Dokan;

using log4net;

using Codice.CM.Common;
using Codice.Client.Commands;
using Codice.Client.Commands.WkTree;

namespace Codice.Client.PlasticDrive
{
    class InterceptFileSystem : DokanOperations
    {
        private static readonly ILog log = LogManager.GetLogger("FileSystemOperations");

        private string mStorageRoot;
        private string mMountPoint;
        private int count_;

        private PlasticAPI mPlasticAPI = new PlasticAPI();
        private FileHandles mHandles = new FileHandles();
        private WorkspaceTreeNode mWorkspaceRoot = null;

        public InterceptFileSystem(string root, string mountPoint)
        {
            mStorageRoot = root;
            mMountPoint = mountPoint;
            count_ = 1;
        }

        internal void Init()
        {
            mWorkspaceRoot = mPlasticAPI.GetWorkspaceTree(mMountPoint);
        }

        private WorkspaceTreeNode GetItem(string path)
        {
            if (path == @"\")
                return mWorkspaceRoot;

            string[] levels = path.Split('\\');

            WorkspaceTreeNode result = mWorkspaceRoot;

            for (int i = 1; i < levels.Length; ++i)
            {
                result = GetChildren(result, levels[i]);

                if (result == null)
                    return null;
            }

            return result;
        }

        private WorkspaceTreeNode GetChildren(WorkspaceTreeNode dir, string name)
        {
            if (dir.Children == null)
                return null;

            foreach (WorkspaceTreeNode child in dir.Children)
            {
                if ((child.Name == name) || (name == "*"))
                    return child;
            }
            return null;
        }

        private string GetPath(string filename)
        {
            return mStorageRoot + filename;
        }

        public int CreateFile(
            string filename,
            FileAccess access,
            FileShare share,
            FileMode mode,
            FileOptions options,
            DokanFileInfo info)
        {
            string path = GetPath(filename);

            bool bCreate = (mode == FileMode.CreateNew || mode == FileMode.Create);

            if( File.Exists(path) && !bCreate )
            {
                info.Context = mHandles.OpenFile(path, access, share, mode, options);

                log.DebugFormat("Create file {0}. Handle [{1}]. Mode [{2}]. Access [{3}]",
                    filename, info.Context, mode, access);
                return 0;
            }
            else if (Directory.Exists(path))
            {
                info.Context = count_++;

                info.IsDirectory = true;
                return 0;
            }
            else if( bCreate )
            {
                // create a new file
                log.DebugFormat("Creating a new file {0}", filename);

                info.Context = mHandles.OpenFile(path, FileAccess.Write, share, mode, options);

                log.DebugFormat("Create file {0}. Handle [{1}]. Mode [{2}]. Access [{3}]",
                    filename, info.Context, mode, access);

                return DokanNet.DOKAN_SUCCESS;
            }
            else
            {
                return -DokanNet.ERROR_FILE_NOT_FOUND;
            }
        }

        public int OpenDirectory(
            string filename,
            DokanFileInfo info)
        {
            log.DebugFormat("OpenDirectory {0}", filename);
            info.Context = count_++;
            if (Directory.Exists(GetPath(filename)))
                return 0;
            else
                return -DokanNet.ERROR_PATH_NOT_FOUND;
        }

        public int CreateDirectory(
            string filename,
            DokanFileInfo info)
        {
            log.DebugFormat("CreateDirectory {0}", filename);

            Directory.CreateDirectory(GetPath(filename));

            return 0;
        }

        public int Cleanup(
            string filename,
            DokanFileInfo info)
        {
            log.DebugFormat("Cleanup {0}", filename);

            if (info.Context == null)
                return -1;

            /*if( ! info.IsDirectory )
                mHandles.Close((long)info.Context);*/

            return 0;
        }

        public int CloseFile(
            string filename,
            DokanFileInfo info)
        {
            log.DebugFormat("CloseFile {0}", filename);

            if (!info.IsDirectory && (info.Context != null)) 
                mHandles.Close((long)info.Context);

            return 0;
        }

        public int ReadFile(
            string filename,
            Byte[] buffer,
            ref uint readBytes,
            long offset,
            DokanFileInfo info)
        {
            if (info.IsDirectory)
                return -1;

            log.DebugFormat("> ReadFile {0}. offset {1}. readBytes {2}", filename, offset, readBytes);

            try
            {
                FileStream fs = mHandles.GetStream((long)info.Context);
                fs.Seek(offset, SeekOrigin.Begin);
                readBytes = (uint)fs.Read(buffer, 0, buffer.Length);
                return 0;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public int WriteFile(
            string filename,
            Byte[] buffer,
            ref uint writtenBytes,
            long offset,
            DokanFileInfo info)
        {
            log.DebugFormat("WriteFile {0}. Handle [{1}]", filename, (long)info.Context);

            FileStream fs = null;
            try
            {
                fs = mHandles.GetStream((long)info.Context);

                if (!fs.CanWrite)
                {
                    log.DebugFormat("Creating a writable handle for [{0}]", filename);
                    mHandles.Close((long)info.Context);
                    // create a writable handle
                    info.Context = mHandles.OpenFile(GetPath(filename), FileAccess.ReadWrite, FileShare.ReadWrite, FileMode.Open, FileOptions.None);
                    fs = mHandles.GetStream((long)info.Context);


                    // and checkout
                    WorkspaceTreeNode item = GetItem(filename);

                    if (item != null && !item.RevInfo.CheckedOut)
                    {
                        item.RevInfo = mPlasticAPI.Checkout("n:" + filename, "automatic checkout").RevInfo;
                    }

                }

                fs.Seek(offset, SeekOrigin.Begin);

                fs.Write(buffer, 0, buffer.Length);

                writtenBytes = (uint)buffer.Length;

                return 0;

            }
            catch (Exception e )
            {
                Console.WriteLine("Error writing to file {0}. Handle [{1}]. [{2}]. {3}",
                    filename,
                    (long)info.Context,
                    fs != null ? fs.Name : "",
                    e.Message);
                return -1;
            }
        }

        public int FlushFileBuffers(
            string filename,
            DokanFileInfo info)
        {
            log.DebugFormat("FlushFileBuffers {0}", filename);

            if (info.IsDirectory)
                return 0;

            FileStream fs = mHandles.GetStream((long)info.Context);
            fs.Flush();

            return 0;
        }

        public int GetFileInformation(
            string filename,
            FileInformation fileinfo,
            DokanFileInfo info)
        {
            log.DebugFormat("GetFileInformation {0}", filename);
            string path = GetPath(filename);
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
            else if (Directory.Exists(path))
            {
                DirectoryInfo f = new DirectoryInfo(path);

                fileinfo.Attributes = f.Attributes;
                fileinfo.CreationTime = f.CreationTime;
                fileinfo.LastAccessTime = f.LastAccessTime;
                fileinfo.LastWriteTime = f.LastWriteTime;
                fileinfo.Length = 0;// f.Length;
                return 0;
            }
            else
            {
                return -1;
            }
        }

        public int FindFiles(
            string filename,
            ArrayList files,
            DokanFileInfo info)
        {
            log.DebugFormat("FindFiles {0}", filename);
            string path = GetPath(filename);
            if (Directory.Exists(path))
            {
                if (filename != @"\")
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

                    /*fi = new FileInformation();
                    fi.Attributes = FileAttributes.Directory | FileAttributes.System;
                    fi.CreationTime = DateTime.Now;
                    fi.LastAccessTime = DateTime.Now;
                    fi.LastWriteTime = DateTime.Now;
                    fi.Length = 0;
                    fi.FileName = "..";
                    files.Add(fi);*/

                }

                DirectoryInfo d = new DirectoryInfo(path);
                FileSystemInfo[] entries = d.GetFileSystemInfos();
                foreach (FileSystemInfo f in entries)
                {
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
            else
            {
                return -1;
            }
        }

        public int SetFileAttributes(
            string filename,
            FileAttributes attr,
            DokanFileInfo info)
        {
            log.DebugFormat("SetFileAttributes {0}", filename);
            return 0;
        }

        public int SetFileTime(
            string filename,
            DateTime ctime,
            DateTime atime,
            DateTime mtime,
            DokanFileInfo info)
        {
            log.DebugFormat("SetFileTime {0}", filename);
            return 0;
        }

        public int DeleteFile(
            string filename,
            DokanFileInfo info)
        {
            log.DebugFormat("DeleteFile {0}", filename);
            return -1;
        }

        public int DeleteDirectory(
            string filename,
            DokanFileInfo info)
        {
            log.DebugFormat("DeleteDirectory {0}", filename);
            return -1;
        }

        public int MoveFile(
            string filename,
            string newname,
            bool replace,
            DokanFileInfo info)
        {
            string src = GetPath(filename);
            string dst = GetPath(newname);

            log.DebugFormat("MoveFile from {0} to {1}", src, dst);

            Console.WriteLine("Going to move");
            WorkspaceTreeNode srcNode = GetItem(filename);

            if (mWorkspaceRoot == null)
            {
                Console.WriteLine("root is null");
            }

            if (srcNode != null)
            {
                Console.WriteLine("Src node is not null");
                try
                {
                    WorkspaceTreeNode srcItem = GetItem(Path.GetDirectoryName(filename));
                    WorkspaceTreeNode dstItem = GetItem(Path.GetDirectoryName(newname));

                    if (srcItem != null && dstItem != null)
                    {
                        if (!srcItem.RevInfo.CheckedOut)
                        {
                            srcItem.RevInfo = mPlasticAPI.Checkout("n:" + Path.GetDirectoryName(filename), "checkout to move").RevInfo;
                        }

                        if (!dstItem.RevInfo.CheckedOut)
                        {
                            dstItem.RevInfo = mPlasticAPI.Checkout("n:" + Path.GetDirectoryName(newname), "checkout to move").RevInfo;
                        }
                    }

                    mPlasticAPI.Move("n:" + filename, "n:" + newname);
                }
                catch (Exception e)
                {
                    log.ErrorFormat("Error moving {0} to {1}. {2}", filename, newname, e.Message);
                    return -1;
                }
            }

            if (File.Exists(src))
                File.Move(src, dst);
            else if (Directory.Exists(src))
                Directory.Move(src, dst);
            else
                return -1;

            return 0;
        }

        public int SetEndOfFile(
            string filename,
            long length,
            DokanFileInfo info)
        {
            log.DebugFormat("SetEndOfFile {0}", filename);
            return -1;
        }

        public int SetAllocationSize(
            string filename, long length, DokanFileInfo info)
        {
            return -1;
        }

        public int LockFile(
            string filename,
            long offset,
            long length,
            DokanFileInfo info)
        {
            log.DebugFormat("LockFile {0}", filename);
            return 0;
        }

        public int UnlockFile(
            string filename,
            long offset,
            long length,
            DokanFileInfo info)
        {
            log.DebugFormat("UnlockFile {0}", filename);
            return 0;
        }

        public int GetDiskFreeSpace(
            ref ulong freeBytesAvailable,
            ref ulong totalBytes,
            ref ulong totalFreeBytes,
            DokanFileInfo info)
        {
            freeBytesAvailable = 512 * 1024 * 1024;
            totalBytes = 1024 * 1024 * 1024;
            totalFreeBytes = 600 * 1024 * 1024;
            return 0;
        }

        public int Unmount(
            DokanFileInfo info)
        {
            return 0;
        }
    }
}
