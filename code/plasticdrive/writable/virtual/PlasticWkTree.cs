using System;
using System.IO;

using Dokan;
using log4net;

using PlasticDrive.Writable.Tree;
using PlasticDrive.Writable.WkTree;
using System.Collections.Generic;

namespace PlasticDrive.Writable.Virtual
{
    class PlasticWkTree: IVirtualFile
    {
        internal PlasticWkTree(
            Node node,
            PlasticFileSystem plasticFileSystem,
            WorkspaceLocalFiles localStorage,
            FileHandles handles)
        {
            mNode = node;
            mPlasticFileSystem = plasticFileSystem;
            mLocalStorage = localStorage;
            mFileHandles = handles;
        }

        int IVirtualFile.CreateFile(
            FileAccess access,
            FileShare share,
            FileMode mode,
            FileOptions options)
        {
            lock (this)
            {
                if (!mLocalStorage.FileExists(mNode.GetNodeId()))
                    CreateNewPlasticWkTreeFile(
                        mNode.GetNodeId(),
                        mPlasticFileSystem.GetWorkspaceContent(),
                        mLocalStorage,
                        mFileHandles);

                int handle = mLocalStorage.OpenFile(
                    mNode.GetNodeId(), "plastic.wktree", access, share, mode, options);

                if (access == FileAccess.Read)
                    mLog.InfoFormat("[{0}] plastic.wktree opened for read",
                        handle);
                else
                    mLog.InfoFormat("*** [{0}] plastic.wktree opened for write",
                        handle);

                return handle;
            }
        }

        int IVirtualFile.CloseFile(int fileHandle)
        {
            lock (this)
            {
                mLog.DebugFormat("CloseFile [{0}]", fileHandle);

                FileStream st = mFileHandles.GetStream(fileHandle);

                bool bCheckForChanges = st.CanWrite;

                mFileHandles.Close(fileHandle);

                if (bCheckForChanges)
                {
                    ReloadPlasticWkTree(mNode.GetNodeId(), mPlasticFileSystem);
                }

                return 0;
            }
        }

        void ReloadPlasticWkTree(
            uint nodeId,
            PlasticFileSystem plasticFileSystem)
        {
            mLog.InfoFormat("Changes detected in plastic.wktree");

            //mPlasticFileSystem.NotifyPlasticWkTreeChanged(nodeId);
        }

        static void CreateNewPlasticWkTreeFile(
            uint nodeId,
            WorkspaceContent wkContent,
            WorkspaceLocalFiles localFiles,
            FileHandles fileHandles)
        {
            mLog.Info("Created plastic.wktree");

            int handle = localFiles.CreateNewFile(
                nodeId,
                "plastic.wktree",
                FileAccess.Write,
                FileShare.Write,
                FileMode.CreateNew,
                FileOptions.None);

            FileStream st = fileHandles.GetStream(handle);

            SerializeWorkspaceContentAsWorkspaceTree.Serialize(
                st, wkContent);

            fileHandles.Close(handle);
        }

        Node mNode;
        PlasticFileSystem mPlasticFileSystem;
        WorkspaceLocalFiles mLocalStorage;
        FileHandles mFileHandles;

        static readonly ILog mLog = LogManager.GetLogger("plastic.wktree");
    }
}
