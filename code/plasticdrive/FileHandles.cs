using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using log4net;

namespace PlasticDrive
{
    internal class FileHandles
    {
        internal int OpenFile(
            string physicalFileName,
            string logicalFileName,
            FileAccess access,
            FileShare share,
            FileMode mode,
            FileOptions options)
        {
            try
            {
                int handle = GetNewHandle();

                FileHandle fileHandle = new FileHandle(
                    handle, physicalFileName, logicalFileName,
                    access, share, mode, options);

                fileHandle.GetStream(); // created the file or opens it

                AddHandle(handle, fileHandle);

                return handle;
            }
            catch (Exception e)
            {
                mLog.ErrorFormat(
                    "Error trying to open file [{0}] [logical: {1}] {2} " +
                    "access:[{3}] share:[{4}] mode:[{5}]",
                    physicalFileName, logicalFileName, e.Message,
                    access, share, mode);

                CheckFile(physicalFileName);

                return -1;
            }
        }

        internal void Close(int handle)
        {
            FileHandle fileHandle = GetHandle(handle);

            if (fileHandle != null)
            {
                mLog.DebugFormat("Close [{0}] - [{1}] " +
                    "access:[{2}] share:[{3}] mode:[{4}]",
                    handle, fileHandle.LogicalFileName,
                    fileHandle.Access, fileHandle.Share, fileHandle.Mode);

                if (fileHandle.GetStream() != null)
                    fileHandle.GetStream().Flush();

                fileHandle.Close();
            }

            FreeHandle(handle);
        }

        internal void CloseAll()
        {
            lock (mHandles)
            {
                foreach (FileHandle fileHandle in mHandles.Values)
                {
                    fileHandle.Close();
                }
            }
        }

        internal FileStream GetStream(int handle)
        {
            FileHandle fileHandle = GetHandle(handle);

            if (fileHandle == null)
                return null;

            return fileHandle.GetStream();
        }

        internal bool SetFileTime(
            string filename,
            DateTime ctime,
            DateTime atime,
            DateTime mtime)
        {
            FileHandle fileHandle = GetByName(filename);

            if (fileHandle == null)
                return false;

            fileHandle.Close();

            try
            {
                File.SetCreationTime(filename, ctime);
                File.SetLastAccessTime(filename, atime);
                File.SetLastWriteTime(filename, mtime);
            }
            catch (Exception e)
            {
                // sometimes wrong dates are sent :-O
                mLog.WarnFormat("Error SetFileTime {0} {1}", filename, e.Message);
            }

            return true;
        }

        FileHandle GetByName(string filename)
        {
            lock (mHandles)
            {
                foreach (FileHandle file in mHandles.Values)
                {
                    if (file.FileName == filename)
                    {
                        return file;
                    }
                }

                return null;
            }
        }

        int GetNewHandle()
        {
            return Interlocked.Increment(ref mHandleCount);
        }

        void AddHandle(int handle, FileHandle fileHandle)
        {
            lock (mHandles)
            {
                mHandles.Add(handle, fileHandle);
            }
        }

        void FreeHandle(int handle)
        {
            lock (mHandles)
            {
                if( mHandles.ContainsKey(handle) )
                    mHandles.Remove(handle);
            }
        }

        void CheckFile(string filename)
        {
            lock (mHandles)
            {
                int count = 0;
                foreach (FileHandle fileHandle in mHandles.Values)
                {
                    if (fileHandle.FileName == filename)
                    {
                        mLog.ErrorFormat(
                            "CheckFile: handle [{0}] opened to [{1}]. Count:{2}. "+
                            "access:[{3}] share:[{4}] mode:[{5}]",
                           fileHandle.Handle, fileHandle.LogicalFileName, ++count,
                           fileHandle.Access, fileHandle.Share, fileHandle.Mode);
                    }
                }
            }
        }

        FileHandle GetHandle(int handle)
        {
            lock (mHandles)
            {
                FileHandle result;

                if (mHandles.TryGetValue(handle, out result))
                    return result;

                return null;
            }
        }

        Dictionary<int, FileHandle> mHandles = new Dictionary<int, FileHandle>();

        int mHandleCount = 0;

        static readonly ILog mLog = LogManager.GetLogger("FileHandles");

        class FileHandle
        {
            internal readonly int Handle;
            internal readonly string FileName;
            internal readonly string LogicalFileName;
            internal readonly FileAccess Access;
            internal readonly FileShare Share;
            internal readonly FileMode Mode;
            internal readonly FileOptions Options;

            internal FileHandle(
                int handle,
                string fileName,
                string logicalFileName,
                FileAccess access,
                FileShare share,
                FileMode mode,
                FileOptions options)
            {
                Handle = handle;
                FileName = fileName;
                LogicalFileName = logicalFileName;
                Access = access;
                Share = share;
                Mode = mode;
                Options = options;
            }

            internal FileStream GetStream()
            {
                if (mStream == null)
                {
                    mStream = new FileStream(
                        FileName, Mode, Access, Share, 1024, Options);
                }

                return mStream;
            }

            internal void Close()
            {
                if (mStream == null)
                    return;

                mStream.Close();

                mStream = null;
            }

            FileStream mStream = null;
        }
    }
}
