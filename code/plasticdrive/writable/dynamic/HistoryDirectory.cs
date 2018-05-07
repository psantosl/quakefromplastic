using System;
using System.IO;
using System.Collections.Generic;

using log4net;

using Codice.CM.Common;
using Codice.Client.Common;

using PlasticDrive.Writable.Tree;
using Codice.Client.Commands;

namespace PlasticDrive.Writable.Dynamic
{
    class HistoryDirectory
    {
        internal bool CreateNewDir(
            string fileName,
            FileAccess access,
            Node tree,
            PlasticAPI api)
        {
            if (!fileName.EndsWith(".hist"))
                return false;

            if (access == FileAccess.ReadWrite || access == FileAccess.Write)
                return false;

            lock (this)
            {
                Node me = WalkTree.Find(tree, fileName);

                if (me != null)
                {
                    ToDelete tmpNode;

                    if (!mOpenHistoryDirectories.TryGetValue(me.GetNodeId(), out tmpNode))
                    {
                        mLog.DebugFormat("************** OUCH!!!!!!!!! Can't CreateNewDir with existing [[{0}]]", me.GetName());
                        return false;
                    }

                    mLog.DebugFormat("************** Adding ref [[{0}]]", fileName);

                    ++tmpNode.RefCount;

                    return true;
                }

                Node versioned = GetControlledNode(fileName, tree);

                if (versioned == null)
                    return false;

                Node parent = WalkTree.Find(tree, Path.GetDirectoryName(fileName));

                string nodeName = Path.GetFileName(fileName);

                // add an item to the tree so "cd myfile.hist" works

                mLog.DebugFormat("************** Adding node [[{0}]]", fileName);

                Node dynamicChild = parent.CreateChild();

                dynamicChild.InitPrivate(
                    DateTime.Now, nodeName, FileAttributes.Directory);

                ToDelete delete = new ToDelete();
                delete.RefCount = 1;
                delete.Parent = parent;
                delete.Child = dynamicChild;

                mOpenHistoryDirectories.Add(dynamicChild.GetNodeId(), delete);

                return true;
            }
        }

        internal bool OpenExisting(Node node)
        {
            lock (this)
            {
                ToDelete tmpNode;

                if (!mOpenHistoryDirectories.TryGetValue(node.GetNodeId(), out tmpNode))
                {
                    mLog.DebugFormat("************** OUCH!!!!!!!!! Can't OpenExisting [[{0}]]", node.GetName());
                    return false;
                }

                mLog.DebugFormat("************** Reopening [[{0}]]", node.GetName());

                ++tmpNode.RefCount;

                return true;
            }
        }

        internal bool Close(Node node)
        {
            ToDelete delete;

            lock (this)
            {
                if (!mOpenHistoryDirectories.TryGetValue(node.GetNodeId(), out delete))
                    return false;

                --delete.RefCount;

                if (delete.RefCount > 0)
                    return true;

                mLog.DebugFormat("************** Deleting [[{0}]]", delete.Child.GetName());

                mOpenHistoryDirectories.Remove(node.GetNodeId());

                delete.Parent.DeleteChild(delete.Child);

                return true;
            }
        }

        internal static int GetFileInformation(
            string fileName,
            Dokan.FileInformation fileinfo,
            Node tree,
            PlasticAPI api)
        {
            if (fileName.IndexOf(".hist") < 0)
                return -1;

            Node node = GetControlledNode(fileName, tree);

            if (node == null)
                return -1;

            fileinfo.FileName = Path.GetFileName(fileName);
            fileinfo.Attributes = FileAttributes.Directory;
            fileinfo.CreationTime = DateTime.Now;
            fileinfo.LastAccessTime = DateTime.Now;
            fileinfo.LastWriteTime = DateTime.Now;
            fileinfo.Length = 0;

            return 0;
        }

        internal static bool FindFiles(
            string fileName,
            Node tree,
            PlasticAPI api,
            List<Dokan.FileInformation> result)
        {
            if (!fileName.EndsWith(".hist"))
                return false;

            Node node = GetControlledNode(fileName, tree);

            if (node == null)
                return false;

            ItemHistory hist = api.GetFileHistory(node.GetRepSpec(), node.GetItemId());

            if (hist == null)
                return false;

            mLog.DebugFormat("************** Listing [[{0}]]", fileName);

            foreach (HistoryRevision rev in hist.Revisions)
            {
                Dokan.FileInformation fileinfo = new Dokan.FileInformation();
                node.FillFileInformation(fileinfo);

                fileinfo.FileName += "#cs:" + rev.ChangeSet + "-" + rev.Owner.Data;
                fileinfo.CreationTime = rev.LocalTimeStamp;
                fileinfo.LastAccessTime = rev.LocalTimeStamp;
                fileinfo.LastWriteTime = rev.LocalTimeStamp;
                fileinfo.Length = rev.Size;

                result.Add(fileinfo);
            }

            return true;
        }

        static Node GetControlledNode(string fileName, Node tree)
        {
            string path = fileName.Substring(0, fileName.IndexOf(".hist"));

            Node result = WalkTree.Find(tree, path);

            if (result.IsControlled())
                return result;

            return null;
        }

        class ToDelete
        {
            internal int RefCount;
            internal Node Parent;
            internal Node Child;
        }

        Dictionary<uint, ToDelete> mOpenHistoryDirectories = new Dictionary<uint, ToDelete>();

        static readonly ILog mLog = LogManager.GetLogger("HistoryDirectory");
    }
}
