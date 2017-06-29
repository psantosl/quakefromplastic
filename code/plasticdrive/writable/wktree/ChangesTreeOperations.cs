using Codice.Client.Commands.Tree;
using Codice.CM.Common.Tree;
using Codice.CM.WorkspaceServer.Tree;
using PlasticDrive.Writable.Tree;

namespace PlasticDrive.Writable.WkTree
{
    class ChangesTreeOperations : TreeOperations
    {
        internal ChangesTreeOperations(WorkspaceContent content)
        {
            mWorkspaceContent = content;
        }

        protected override TreeChangedNode BuildTreeChangedNodeFromTreeNode(object treeNode)
        {
            return BuildChangedNodeFromNode((Node)treeNode);
        }

        public override void CleanChanges()
        {
            mWorkspaceContent.CleanChangesTree();
        }

        protected override TreeChangedNode GetCurrentChangesTree()
        {
            return mWorkspaceContent.GetChangesTree();
        }

        protected override object GetTreeChildByName(string name, object treeNode)
        {
            Node node = (Node)treeNode;

            if (node.GetChildrenCount() == 0)
                return null;

            return WalkTree.GetChildByName(node.GetChildren(), name);
        }

        protected override TreeChangedNode ProcessTreeForNode(string cmpath)
        {
            Node tree = mWorkspaceContent.GetTree();
            TreeChangedNode changesTree = mWorkspaceContent.GetChangesTree();

            if(changesTree == null)
            {
                changesTree = BuildChangedNodeFromNode(tree);
                mWorkspaceContent.InitializeChangesTree(changesTree);
            }

            return ProcessTreeForNode(changesTree, tree, cmpath);
        }

        static TreeChangedNode BuildChangedNodeFromNode(Node node)
        {
            TreeChangedNode result = new TreeChangedNode(
                node.GetName(), node.CloneRevisionInfo(), TreeNodeStatus.Normal);
            result.Xlink = node.GetXlink();
            return result;
        }

        WorkspaceContent mWorkspaceContent;
    }
}
