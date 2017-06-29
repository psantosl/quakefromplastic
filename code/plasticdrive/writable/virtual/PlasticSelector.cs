using System;
using System.IO;
using System.Text;

using log4net;
using Dokan;

using PlasticDrive.Writable.Tree;
using PlasticDrive.Writable.WkTree;
using System.Collections.Generic;

namespace PlasticDrive.Writable.Virtual
{
    class PlasticSelector : IVirtualFile
    {
        internal PlasticSelector(
            Node node,
            Node rootNode,
            PlasticAPI plasticApi,
            WorkspaceLocalFiles tmpStorage,
            FileHandles handles,
            PlasticFileSystem fileSystem)
        {
            mNode = node;
            mLocalStorage = tmpStorage;
            mFileHandles = handles;
            mPlasticFileSystem = fileSystem;

            StringBuilder b = new StringBuilder();

            b.AppendFormat("rep \"{0}\"", rootNode.GetRepSpec().ToString());
            b.AppendLine(  "  path \"/\"");
            b.AppendFormat("    smartbranch \"{0}\"",
                plasticApi.GetBranchName(rootNode.GetRepSpec(), rootNode.GetBranchId()));

            mSelector = b.ToString();
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
                    CreateNewSelectorFile(
                        mSelector,
                        mNode.GetNodeId(),
                        mLocalStorage,
                        mFileHandles);

                int handle = mLocalStorage.OpenFile(
                    mNode.GetNodeId(),
                    "plastic.selector",
                    access,
                    share,
                    mode,
                    options);

                mLog.DebugFormat("OpenFile [{0}] access:[{1}] mode:[{2}]",
                    handle, access, mode);

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

                if (!bCheckForChanges)
                    return 0;

                // let's see if the selector changed
                HandleSelectorChange(
                    mNode.GetNodeId(),
                    mLocalStorage,
                    mFileHandles,
                    mPlasticFileSystem,
                    ref mSelector);

                return 0;
            }
        }

        static void CreateNewSelectorFile(
            string selectorText,
            uint nodeId,
            WorkspaceLocalFiles localFiles,
            FileHandles fileHandles)
        {
            int handle = localFiles.CreateNewFile(
                nodeId,
                "plastic.selector",
                FileAccess.Write,
                FileShare.Write,
                FileMode.CreateNew,
                FileOptions.None);

            FileStream st = fileHandles.GetStream(handle);

            StreamWriter writer = new StreamWriter(st);
            writer.Write(selectorText);
            writer.Flush();

            fileHandles.Close(handle);
        }

        static void HandleSelectorChange(
            uint nodeId,
            WorkspaceLocalFiles localFiles,
            FileHandles fileHandles,
            PlasticFileSystem plasticFileSystem,
            ref string currentSelector)
        {
            int handle = localFiles.OpenFile(
                nodeId,
                "plastic.selector",
                FileAccess.Read, FileShare.ReadWrite, FileMode.Open, FileOptions.None);
            try
            {
                FileStream st = fileHandles.GetStream(handle);

                StreamReader reader = new StreamReader(st);

                string newSelector = reader.ReadToEnd();

                mLog.DebugFormat("New selector is [{0}]", newSelector);

                if (currentSelector == newSelector)
                    return;

                mLog.InfoFormat("Detected selector change");

                currentSelector = newSelector;

                plasticFileSystem.NotifySelectorChanged(currentSelector);
            }
            finally
            {
                fileHandles.Close(handle);
            }
        }

        string mSelector;

        Node mNode;
        WorkspaceLocalFiles mLocalStorage;
        FileHandles mFileHandles;
        PlasticFileSystem mPlasticFileSystem;

        static readonly ILog mLog = LogManager.GetLogger("plastic.selector");
    }
}