using System.IO;

using Dokan;
using log4net;

using PlasticDrive.Writable.Tree;
using System.Collections.Generic;
using Codice.CM.WorkspaceServer;

namespace PlasticDrive.Writable.Virtual
{
    class PlasticChanges : IVirtualFile
    {
        internal PlasticChanges(
            uint nodeId,
            WorkspaceContent wkContent,
            WorkspaceLocalFiles tmpStorage,
            FileHandles handles)
        {
            mNodeId = nodeId;
            mWkContent = wkContent;
            mLocalFiles = tmpStorage;
            mFileHandles = handles;
        }

        int IVirtualFile.CreateFile(
            FileAccess access,
            FileShare share,
            FileMode mode,
            FileOptions options)
        {
            if (mode != FileMode.Open)
                return -DokanNet.ERROR_ACCESS_DENIED;

            if (access != FileAccess.Read)
                return -DokanNet.ERROR_ACCESS_DENIED;

            lock (this)
            {
                if (!mLocalFiles.FileExists(mNodeId))
                    CreateNewPlasticChangesFile(
                        mNodeId,
                        mWkContent.GetChangesTree(),
                        mLocalFiles,
                        mFileHandles);

                int handle = mLocalFiles.OpenFile(
                    mNodeId, "plastic.changes", access, share, mode, options);

                mOpenHandles.Add(handle);

                mLog.DebugFormat("OpenFile [{0}] access:[{1}] mode:[{2}]",
                    handle, access, mode);

                return handle;
            }
        }

        int IVirtualFile.CloseFile(int fileHandle)
        {
            lock (this)
            {
                mFileHandles.Close(fileHandle);

                mOpenHandles.Remove(fileHandle);

                if (mOpenHandles.Count == 0)
                    mLocalFiles.Delete(mNodeId);

                return 0;
            }
        }

        static void CreateNewPlasticChangesFile(
            uint nodeId,
            Codice.Client.Commands.Tree.TreeChangedNode changedTree,
            WorkspaceLocalFiles localFiles,
            FileHandles fileHandles)
        {
            int handle = localFiles.CreateNewFile(
                nodeId,
                "plastic.changes",
                FileAccess.Write,
                FileShare.Write,
                FileMode.CreateNew,
                FileOptions.None);

            FileStream st = fileHandles.GetStream(handle);
            TreeChangedDataStore.SerializeChangedTree(st, changedTree);

            fileHandles.Close(handle);
        }

        List<int> mOpenHandles = new List<int>();

        WorkspaceContent mWkContent;
        uint mNodeId;
        WorkspaceLocalFiles mLocalFiles;
        FileHandles mFileHandles;

        static readonly ILog mLog = LogManager.GetLogger("plastic.changes");
    }
}
