using System;
using System.IO;
using System.Collections.Generic;

using Dokan;
using PlasticDrive.Writable.Tree;
using PlasticDrive.Writable.WkTree;

namespace PlasticDrive.Writable.Virtual
{
    class PlasticWorkspace: IVirtualFile
    {
        internal PlasticWorkspace(
            Guid wkId,
            string wkName,
            Node node,
            WorkspaceLocalFiles tmpStorage,
            FileHandles handles)
        {
            mWorkspaceId = wkId;
            mWorkspaceName = wkName;

            mNode = node;
            mTmpStorage = tmpStorage;
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
                int handle;

                if (mOpenHandles.Count == 0)
                {
                    handle = mTmpStorage.CreateNewFile(
                        mNode.GetNodeId(),
                        "plastic.workspace",
                        FileAccess.Write,
                        FileShare.Write,
                        FileMode.CreateNew,
                        FileOptions.None);

                    FileStream st = mFileHandles.GetStream(handle);

                    StreamWriter writer = new StreamWriter(st);

                    writer.WriteLine(mWorkspaceName);
                    writer.WriteLine(mWorkspaceId);
                    writer.Flush();

                    mFileHandles.Close(handle);
                }

                handle = mTmpStorage.OpenFile(
                    mNode.GetNodeId(), "plastic.workspace", access, share, mode, options);

                mOpenHandles.Add(handle);

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
                    mTmpStorage.Delete(mNode.GetNodeId());

                return 0;
            }
        }

        List<int> mOpenHandles = new List<int>();

        Guid mWorkspaceId;
        string mWorkspaceName;
        Node mNode;
        WorkspaceLocalFiles mTmpStorage;
        FileHandles mFileHandles;
    }
}
