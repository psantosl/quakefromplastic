using System;
using System.IO;

using Codice.Client.Commands;
using Codice.CM.Common.Tree;
using Codice.CM.Common;
using Codice.Client.Commands.Xlinks;

namespace PlasticDrive.Writable.Tree
{
    static class CreateWorkspaceContentFromSelector
    {
        // remark: ideally we could do this during custom de-serialization
        // of the TreeContent, so that we don't even create the intermediate
        // objects
        internal static WorkspaceContent Create(DateTime now, TreeContent selectorTree)
        {
            Node tree = new Node();

            RepositorySpec repSpec =
                selectorTree.GetRepositoryInfo(InternalNames.RootDir).GetRepSpec();

            Assign(repSpec, selectorTree, now, tree, selectorTree.Tree);

            return new WorkspaceContent(tree, selectorTree.ChangesetId);
        }

        static void Assign(
            RepositorySpec repSpec,
            TreeContent selectorTree,
            DateTime now,
            Node node,
            TreeNode selectorNode)
        {
            ClientXlink xlink = XlinkTreeNode.GetXlink(selectorNode) as ClientXlink;

            node.InitControlled(
                now,
                selectorNode.Name,
                xlink != null || selectorNode.IsDirectory() ?
                    FileAttributes.Directory : FileAttributes.Normal,
                selectorNode.RevInfo.Size,
                repSpec, selectorNode.RevInfo, xlink);

            if (!selectorNode.HasChildren)
                return;

            foreach (TreeNode child in selectorNode.Children)
            {
                Node newChild = node.CreateChild();
                Assign(GetChildRepSpec(child, repSpec), selectorTree, now, newChild, child);
            }
        }

        static RepositorySpec GetChildRepSpec(TreeNode child, RepositorySpec parentRepSpec)
        {
            if (!XlinkTreeNode.IsXlinkNode(child))
                return parentRepSpec;

            Xlink xlink = XlinkTreeNode.GetXlink(child);
            if (!xlink.RelativeServer && !xlink.RelativeRepository)
                return xlink.RepSpec;

            RepositorySpec result = new RepositorySpec();
            result.Server = xlink.RelativeServer ? parentRepSpec.Server : xlink.RepSpec.Server;
            result.Name = xlink.RelativeRepository ? parentRepSpec.Name : xlink.RepSpec.Name;
            return result;
        }
    }
}
