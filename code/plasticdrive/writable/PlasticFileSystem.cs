using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using Dokan;
using log4net;

using PlasticDrive.Writable.Tree;
using PlasticDrive.Writable.Virtual;
using PlasticDrive.Writable.WkTree;

namespace PlasticDrive.Writable
{
    class PlasticFileSystem: DokanOperations, IPlasticFs
    {
        internal PlasticFileSystem(
            WorkspaceContent content,
            string cachePath,
            PlasticAPI plasticApi,
            FileHandles handles,
            WorkspaceLocalFiles tempStorage,
            VirtualFiles virtualFiles)
        {
            mWorkspaceContent = content;
            mChangesTreeOperations = new ChangesTreeOperations(content);
            mLocalFilesPath = cachePath;
            mFileCache = new FileCache(mLocalFilesPath);
            mPlasticApi = plasticApi;
            mHandles = handles;
            mLocalFiles = tempStorage;
            mVirtualFiles = virtualFiles;
        }

        internal void NotifySelectorChangedB(string newSelector)
        {
            // just launch a thread to do the calculation of the new selector
            // and return immediately

            // comment added on the method on a demo

            ThreadPool.QueueUserWorkItem(ChangeSelector, newSelector);
        }

        internal void NotifyPlasticWkTreeChanged(uint nodeId)
        {
            // just launch a new thread to do the deserialization and update
            ThreadPool.QueueUserWorkItem(ReloadWkTree, nodeId);
        }

        internal WorkspaceContent GetWorkspaceContent()
        {
            lock (this)
            {
                return mWorkspaceContent;
            }
        }

        void ReloadWkTree(object o)
        {
            uint nodeId = (uint) o;

            int handle = -1;
            WorkspaceContent newContent = null;

            try
            {
                handle = mLocalFiles.OpenFile(
                    nodeId,
                    "plastic.wktree",
                    FileAccess.Read,
                    FileShare.Read,
                    FileMode.Open,
                    FileOptions.None);

                FileStream st = mHandles.GetStream(handle);

                newContent = DeserializeWorkspaceContentAsPlasticDriveTree.Deserialize(
                    DateTime.Now, st);
            }
            catch (Exception e)
            {
                mLog.ErrorFormat("ReloadWkTree found error: {0}", e.Message);

                return;
            }
            finally
            {
                if (handle != -1)
                    mHandles.Close(handle);
            }

            try
            {

                mLocalFiles.Delete(nodeId);

                Node newTree = MergeWorkspaceTree.MergeNewTreeWithPrivates(
                    DateTime.Now, newContent.GetTree(), GetRoot());

                lock (this)
                {
#warning check the csetId
                    mWorkspaceContent.ChangeTree(newTree);
                }

                mLog.Info("plastic.wktree reloaded from disk");
            }
            catch (Exception e)
            {
                mLog.ErrorFormat("Error in ReloadWkTree {0}", e.Message);
            }
        }

        void ChangeSelector(object o)
        {
            string newSelector = o as string;

            try
            {
                Codice.Client.Commands.TreeContent treeContent =
                   mPlasticApi.GetSelectorContent(newSelector);

                if (treeContent == null)
                {
                    mLog.ErrorFormat("Can't load selector [{0}]", newSelector);
                    return;
                }

                WorkspaceContent newContent = CreateWorkspaceContentFromSelector.Create(
                    DateTime.Now, treeContent);

                Node newTree = MergeWorkspaceTree.MergeNewTreeWithPrivates(
                    DateTime.Now, newContent.GetTree(), GetRoot());

                lock (this)
                {
#warning check the csetId
                    mWorkspaceContent.ChangeTree(newTree);
                }

                mLog.InfoFormat("Selector correctly changed to {0}", newSelector);
            }
            catch (Exception e)
            {
                mLog.ErrorFormat("Can't load selector [{0}]. Error loading {1}",
                    newSelector, e.Message);
            }
        }

        bool IPlasticFs.IsInitialized()
        {
            return true;
        }

        void IPlasticFs.Stop()
        {
            mHandles.CloseAll();
            mLocalFiles.Close();
        }

        int DokanOperations.CreateFile(
            string path,
            FileAccess access,
            FileShare share,
            FileMode mode,
            FileOptions options,
            DokanFileInfo info)
        {
            mLog.DebugFormat(
                "CreateFile - mode: {0, 5} - options: {1, 5} - {2}",
                mode, options, path);

            Node tree = GetRoot();

            Node node = WalkTree.Find(tree, path);
            if (node == null)
            {
                int error;
                bool bIsDirectory;

                int handle = CreateNew.Create(
                    path,
                    access,
                    share,
                    mode,
                    options,
                    tree,
                    mFileCache,
                    mHandles,
                    mPlasticApi,
                    mLocalFiles,
                    mHistoryDirectories,
                    out bIsDirectory,
                    out error);

                if (error != 0)
                    return error;

                if (bIsDirectory)
                    DirectoryContext.Set(info);
                else
                FileContext.Set(info, handle);

                return 0;
            }

            if (mHistoryDirectories.OpenExisting(node))
            {
                DirectoryContext.Set(info);
                return 0;
            }

            if (node.IsDirectory())
            {
                DirectoryContext.Set(info);
                return 0;
            }

            IVirtualFile virtualFile;

            if (( virtualFile = mVirtualFiles.Get(node.GetNodeId())) != null)
            {
                int handle = virtualFile.CreateFile(access, share, mode, options);

                FileContext.Set(info, handle);

                return 0;
            }

#warning here me must handle CreateNew which I think should delete the existing file or fail

            if (node.IsControlledAndReadOnly())
            {
                string fileToOpen = mFileCache.GetFile(
                    node.GetRepSpec(),
                    node.CloneRevisionInfo(),
                    path,
                    mPlasticApi);

                if (OpenForRead(access))
                {
                    int handle = mHandles.OpenFile(
                        fileToOpen,
                        path,
                        access, share, mode, options);

                    if (handle == -1)
                        return -1;

                    FileContext.Set(info, handle);

                    return 0;
                }

                // then we need to check it out and copy the content
                // of the file to the writable storage

                mLog.DebugFormat("**Doing CHECKOUT**");

                string writablePath = mLocalFiles.GetPathForFile(node.GetNodeId());

                File.Copy(fileToOpen, writablePath);

                Checkout(node, path, mChangesTreeOperations);
            }

            // return the existing private file
            int fhandle = LocalFile.OpenExisting(
                node.GetNodeId(),
                path,
                access,
                share,
                mode,
                options,
                mLocalFiles);

            if (fhandle == -1)
                return -DokanNet.ERROR_FILE_NOT_FOUND;

            FileContext.Set(info, fhandle);

            return fhandle;
        }

        static bool OpenForRead(FileAccess access)
        {
            return access == FileAccess.Read;
        }

        int DokanOperations.OpenDirectory(string filename, DokanFileInfo info)
        {
            mLog.DebugFormat("OpenDirectory - [{0}]", filename);

            if (mHistoryDirectories.CreateNewDir(filename, FileAccess.Read, GetRoot(), mPlasticApi))
            {
                DirectoryContext.Set(info);
                return 0;
            }

            DirectoryContext.Set(info);

            Node node = WalkTree.Find(GetRoot(), filename);

            return node != null ? 0 : -DokanNet.ERROR_PATH_NOT_FOUND;
        }

        int DokanOperations.CreateDirectory(string filename, DokanFileInfo info)
        {
            mLog.DebugFormat(
                "CreateDirectory - {0}",
                filename);

            Node tree = GetRoot();

            Node node = WalkTree.Find(tree, filename);

            if (node != null)
                return -DokanNet.ERROR_ALREADY_EXISTS;

            // create the private directory in the tree
            Node parent = WalkTree.Find(tree, Path.GetDirectoryName(filename));

            if (parent == null)
                return -DokanNet.ERROR_PATH_NOT_FOUND;

            Node child = parent.CreateChild();

            child.InitPrivate(DateTime.Now, Path.GetFileName(filename), FileAttributes.Directory);

            return 0;
        }

        int DokanOperations.Cleanup(string filename, DokanFileInfo info)
        {
            if (info == null)
            {
                mLog.DebugFormat("Cleanup - {0} - INFO IS NULL", filename);
                return 0;
            }

            long size = 0;

            if (!info.IsDirectory)
            {
                Stream st = mHandles.GetStream(FileContext.Get(info));
                if (st != null)
                    size = st.Length;
            }

            Node node = WalkTree.Find(GetRoot(), filename);

            if (node != null)
            {
                mLog.DebugFormat("Cleanup - [{0}] - new size {1}", filename, size);

                node.UpdateSize(size);
            }
            else
            {
                mLog.DebugFormat("Cleanup - {0}", filename);
            }

            return 0;
        }

        int DokanOperations.CloseFile(string filename, DokanFileInfo info)
        {
            mLog.DebugFormat("CloseFile - {0}", filename);

            Node node = WalkTree.Find(GetRoot(), filename);

            if (node == null)
                return -DokanNet.ERROR_FILE_NOT_FOUND;

            if (mHistoryDirectories.Close(node))
                return 0;

            IVirtualFile virtualFile;

            if ((virtualFile = mVirtualFiles.Get(node.GetNodeId())) != null)
            {
                int handle = FileContext.Get(info);

                if (handle == -1)
                    return 0;

                virtualFile.CloseFile(handle);

                return 0;
            }

            if (info.IsDirectory)
                return 0;

            mHandles.Close(FileContext.Get(info));

            return 0;
        }

        int DokanOperations.ReadFile(
            string filename,
            byte[] buffer,
            ref uint readBytes,
            long offset,
            DokanFileInfo info)
        {
            // this thing sometimes trows exceptions because
            // System.Diagnostics.Process.GetProcessById((int)info.ProcessId).MainModule.FileName,
            // System.ComponentModel.Win32Exception (0x80004005): A 32 bit processes cannot access modules of a 64 bit process.
            // so we need to find a different way to do it

            mLog.DebugFormat(
                "ReadFile - offset: {0, 5} - bytes: {1, 5} - {2}",
                offset, buffer.Length, filename);

            if (info.IsDirectory)
                return -1;

            try
            {
                FileStream fs = mHandles.GetStream(FileContext.Get(info));

                if (fs == null)
                {
                    // some apps (Notepad) don't open the file first!!
                    if ( (this as DokanOperations).CreateFile(
                            filename,
                            FileAccess.Read,
                            FileShare.Read,
                            FileMode.Open,
                            FileOptions.None,
                            info) != 0)
                    {
                        mLog.ErrorFormat("Can't find open file {0}", filename);

                        return -DokanNet.ERROR_PATH_NOT_FOUND;
                    }

                    fs = mHandles.GetStream(FileContext.Get(info));
                }

                if (!fs.CanRead)
                {
                    mLog.ErrorFormat("Can't read from open file [{0}]",
                        filename);
                    return -DokanNet.ERROR_ACCESS_DENIED;
                }

                fs.Seek(offset, SeekOrigin.Begin);
                readBytes = (uint)fs.Read(buffer, 0, buffer.Length);
                return 0;
            }
            catch (Exception e)
            {
                mLog.ErrorFormat("Error reading from {0}. {1}",
                    filename, e.Message);
                return -1;
            }
        }

        int DokanOperations.WriteFile(
            string filename,
            byte[] buffer,
            ref uint writtenBytes,
            long offset,
            DokanFileInfo info)
        {
            mLog.DebugFormat(
                "WriteFile - offset: {0, 5} - bytes: {1, 5} - {2}",
                offset, buffer.Length, filename);

            Node node = WalkTree.Find(GetRoot(), filename);

            if (node == null)
                return -DokanNet.ERROR_PATH_NOT_FOUND;

            if (node.IsControlledAndReadOnly())
            {
                return -DokanNet.ERROR_ACCESS_DENIED;
            }

            return mLocalFiles.WriteFile(
                node.GetNodeId(),
                filename,
                buffer,
                ref writtenBytes,
                offset,
                FileContext.Get(info));
        }

        int DokanOperations.FlushFileBuffers(string filename, DokanFileInfo info)
        {
            mLog.DebugFormat("FlushFileBuffers {0}", filename);

            if (info.IsDirectory)
                return 0;

            FileStream fs = mHandles.GetStream(FileContext.Get(info));

            if (fs != null)
                fs.Flush();

            return 0;
        }

        int DokanOperations.GetFileInformation(
            string filename,
            FileInformation fileinfo,
            DokanFileInfo info)
        {
            mLog.DebugFormat(
                "GetFileInformation - {0}",
                filename);

            Node node = WalkTree.Find(GetRoot(), filename);

            if (node != null)
            {
                node.FillFileInformation(fileinfo);
                return 0;
            }

            int result = Dynamic.SpecNode.GetFileInformation(
                filename, fileinfo, GetRoot(), mPlasticApi);

            if (result == 0)
                return 0;

            result = Dynamic.HistoryDirectory.GetFileInformation(
                filename, fileinfo, GetRoot(), mPlasticApi);

            if (result == 0)
                return 0;

            return -DokanNet.ERROR_PATH_NOT_FOUND;
        }

        int DokanOperations.FindFiles(
            string filename,
            ArrayList files,
            DokanFileInfo info)
        {
            mLog.DebugFormat("FindFiles - [{0}]", filename);

            List<FileInformation> result = new List<FileInformation>();

            if (Dynamic.HistoryDirectory.FindFiles(filename, GetRoot(), mPlasticApi, result))
            {
            }
            else
            {
                Node node = WalkTree.Find(GetRoot(), filename);

                if (node == null)
                    return -DokanNet.ERROR_PATH_NOT_FOUND;

                if (!node.IsDirectory())
                    return 0;

                result = ListTree(node);

                if (node != GetRoot())
                    AddCurrentAndPreviousDirectories(result, node);
            }

            result.Sort((f1, f2) => f1.FileName.CompareTo(f2.FileName));

            files.AddRange(result);

            return 0;
        }

        List<FileInformation> ListTree(Node node)
        {
            List<FileInformation> result = new List<FileInformation>();

            if (node.GetChildren() != null)
            {
                foreach (Node child in node.GetChildren())
                {
                    FileInformation fi = new FileInformation();

                    child.FillFileInformation(fi);

                    result.Add(fi);
                }
            }

            return result;
        }

        int DokanOperations.SetFileAttributes(
            string filename,
            System.IO.FileAttributes attr,
            DokanFileInfo info)
        {
            mLog.DebugFormat("SetFileAttributes - {0}", filename);

            return 0;
        }

        int DokanOperations.SetFileTime(
            string filename,
            System.DateTime ctime,
            System.DateTime atime,
            System.DateTime mtime,
            DokanFileInfo info)
        {
            mLog.DebugFormat("SetFileTime - {0}", filename);

            return 0;
        }

        int DokanOperations.DeleteFile(string filename, DokanFileInfo info)
        {
            mLog.InfoFormat("*DeleteFile* - {0}", filename);

            return Delete(GetRoot(), filename, mVirtualFiles, mChangesTreeOperations);
        }

        int DokanOperations.DeleteDirectory(string filename, DokanFileInfo info)
        {
            mLog.DebugFormat("DeleteDirectory {0}", filename);

            return Delete(GetRoot(), filename, mVirtualFiles, mChangesTreeOperations);
        }

        static int Delete(
            Node root,
            string path,
            VirtualFiles virtualFiles,
            ChangesTreeOperations changesTreeOperations)
        {
            Node parent = WalkTree.Find(root, Path.GetDirectoryName(path));

            if (parent == null)
                return -DokanNet.ERROR_PATH_NOT_FOUND;

            Node node = WalkTree.GetChildByName(parent.GetChildren(), Path.GetFileName(path));

            if (node == null)
                return -DokanNet.ERROR_FILE_NOT_FOUND;

            IVirtualFile virtualFile = virtualFiles.Get(node.GetNodeId());

            if (virtualFile != null)
            {
                mLog.ErrorFormat("We don't allow deleting virtual files. [{0}]",
                    node.GetName());

                return -DokanNet.ERROR_ACCESS_DENIED;
            }

            if (node.IsControlled())
                changesTreeOperations.Delete(CmPath.FromWindows(path));

            if (parent.DeleteChild(node))
                return 0;

            return -DokanNet.ERROR_FILE_NOT_FOUND;
        }

#warning pending to check plastic move restrictions
        // * do not move an xlink
        // * do not move between xlinks
        // * do not move a controlled item to a private path
        //   or add the private parents
        // * etc.
        int DokanOperations.MoveFile(
            string srcPath,
            string dstPath,
            bool replace,
            DokanFileInfo info)
        {
            mLog.InfoFormat(
                "*MoveFile* - [{0}] to [{1}] - replace [{2}]",
                srcPath, dstPath, replace);

            Node tree = GetRoot();

            Node parentSrc = WalkTree.Find(tree, Path.GetDirectoryName(srcPath));

            if (parentSrc == null)
                return -DokanNet.ERROR_PATH_NOT_FOUND;

            Node node = WalkTree.GetChildByName(parentSrc.GetChildren(), Path.GetFileName(srcPath));

            if (node == null)
                return -DokanNet.ERROR_FILE_NOT_FOUND;

            IVirtualFile virtualNode = mVirtualFiles.Get(node.GetNodeId());

            if (virtualNode != null)
            {
                mLog.ErrorFormat("We don't allow moving virtual files. [{0}]",
                    node.GetName());

                return -DokanNet.ERROR_ACCESS_DENIED;
            }

            Node parentDst = WalkTree.Find(tree, Path.GetDirectoryName(dstPath));

            if (parentDst == null)
                return -DokanNet.ERROR_PATH_NOT_FOUND;

            if (WalkTree.GetChildByName(parentDst.GetChildren(), Path.GetFileName(dstPath)) != null)
                return -DokanNet.ERROR_ALREADY_EXISTS;

            if (node.IsControlled())
                mChangesTreeOperations.Move(
                    CmPath.FromWindows(srcPath), CmPath.FromWindows(dstPath));

            parentSrc.DeleteChild(node);

            parentDst.AddChild(node, Path.GetFileName(dstPath));

            return 0;
        }

        int DokanOperations.SetEndOfFile(string filename, long length, DokanFileInfo info)
        {
            mLog.DebugFormat("SetEndOfFile - [{0}] {1}", filename, length);

            Node node = WalkTree.Find(GetRoot(), filename);

            if (node == null)
                return -DokanNet.ERROR_PATH_NOT_FOUND;

            return 0;
        }

        int DokanOperations.SetAllocationSize(string filename, long length, DokanFileInfo info)
        {
            mLog.DebugFormat("SetAllocationSize - [{0}] {1}", filename, length);

            return 0;
        }

        int DokanOperations.LockFile(string filename, long offset, long length, DokanFileInfo info)
        {
            mLog.DebugFormat("LockFile {0}", filename);
            return 0;
        }

        int DokanOperations.UnlockFile(string filename, long offset, long length, DokanFileInfo info)
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
            ulong diskFreeBytes = DiskFreeSpace.Get(mLocalFilesPath);
            ulong controlledSize = WalkTree.GetTotalSize(GetRoot());

            freeBytesAvailable = diskFreeBytes - controlledSize;
            totalBytes = diskFreeBytes;
            totalFreeBytes = diskFreeBytes - controlledSize;

            return 0;
        }

        int DokanOperations.Unmount(DokanFileInfo info)
        {
            (this as IPlasticFs).Stop();
            return 0;
        }

        void AddCurrentAndPreviousDirectories(
            List<FileInformation> files, Node node)
        {
            // add . and .. entries
            FileInformation fi = new FileInformation();
            fi.Attributes = FileAttributes.Directory;
            node.FillDates(fi);
            fi.Length = 0;
            fi.FileName = ".";
            files.Add(fi);

            fi = new FileInformation();
            fi.Attributes = FileAttributes.Directory;
            node.FillDates(fi);
            fi.Length = 0;
            fi.FileName = "..";
            files.Add(fi);
        }

        static class CreateNew
        {
            static internal int Create(
                string filename,
                FileAccess access,
                FileShare share,
                FileMode mode,
                FileOptions options,
                Node tree,
                FileCache fileCache,
                FileHandles fileHandles,
                PlasticAPI api,
                WorkspaceLocalFiles localFiles,
                Dynamic.HistoryDirectory historyDir,
                out bool bIsDirectory,
                out int error)
            {
                error = 0;
                bIsDirectory = false;

                int handle = Dynamic.SpecNode.CreateFile(
                    filename, access, share, mode, options,
                    tree, fileCache, fileHandles, api);

                if (handle != -1)
                    return handle;

                if (historyDir.CreateNewDir(
                    filename, access, tree, api))
                {
                    bIsDirectory = true;
                    return -1;
                }

                return LocalFile.CreateNew(
                    filename, access, share, mode, options, tree, localFiles, out error);
            }
        }

        static class LocalFile
        {
            internal static int CreateNew(
                string filename,
                FileAccess access,
                FileShare share,
                FileMode mode,
                FileOptions options,
                Node tree,
                WorkspaceLocalFiles localFiles,
                out int error)
            {
                // it is a private file, just create it
                Node parent = WalkTree.Find(tree, Path.GetDirectoryName(filename));

                if (parent == null || !parent.IsDirectory())
                {
                    error = -DokanNet.ERROR_PATH_NOT_FOUND;
                    return 0;
                }

                if (!IsCreateMode(mode))
                {
                    error = -DokanNet.ERROR_FILE_NOT_FOUND;
                    return 0;
                }

                Node newNode = parent.CreateChild();

                newNode.InitPrivate(
                    DateTime.Now,
                    Path.GetFileName(filename),
                    FileAttributes.Normal);

                error = 0;

                return localFiles.CreateNewFile(
                    newNode.GetNodeId(),
                    filename,
                    access,
                    share,
                    mode,
                    options);
            }

            internal static int OpenExisting(
                uint nodeId,
                string filename,
                FileAccess access,
                FileShare share,
                FileMode mode,
                FileOptions options,
                WorkspaceLocalFiles tmpStorage)
            {
                return tmpStorage.OpenFile(
                    nodeId,
                    filename,
                    access,
                    share,
                    mode,
                    options);
            }

            static bool IsCreateMode(FileMode mode)
            {
                return (mode == FileMode.Create)
                    || (mode == FileMode.CreateNew)
                    || (mode == FileMode.OpenOrCreate);
            }
        }

        Node GetRoot()
        {
            lock (this)
            {
                return mWorkspaceContent.GetTree();
            }
        }

        static class CmPath
        {
            internal static string FromWindows(string path)
            {
                int unitIndex = path.IndexOf(":");
                if (unitIndex != -1)
                    path = path.Substring(unitIndex + 1);

                return path.Replace(Path.DirectorySeparatorChar, '/');
            }
        }

        static void Checkout(
            Node node,
            string path,
            ChangesTreeOperations changesTreeOperations)
        {
            changesTreeOperations.Checkout(CmPath.FromWindows(path));

            node.Checkout(3, new Codice.CM.Common.SEID("plasticdrive", false));
        }

        ChangesTreeOperations mChangesTreeOperations;

        WorkspaceContent mWorkspaceContent;
        FileCache mFileCache;
        string mLocalFilesPath;
        PlasticAPI mPlasticApi;
        FileHandles mHandles;
        WorkspaceLocalFiles mLocalFiles;
        VirtualFiles mVirtualFiles;
        Dynamic.HistoryDirectory mHistoryDirectories = new Dynamic.HistoryDirectory();

        static readonly ILog mLog = LogManager.GetLogger("PlasticFileSystem");
    }
}
