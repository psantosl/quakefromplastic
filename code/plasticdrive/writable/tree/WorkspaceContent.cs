using System.Collections.Generic;

using Codice.Client.Commands.TransformerRule;
using Codice.Client.Commands.Tree;

namespace PlasticDrive.Writable.Tree
{
    internal class WorkspaceContent
    {
        internal WorkspaceContent(Node tree, long changesetId)
        {
            mTree = tree;
            mChangesetId = changesetId;
        }

        internal Node GetTree()
        {
            lock (this)
            {
                return mTree;
            }
        }

        internal TreeChangedNode GetChangesTree()
        {
            return mChangesTree;
        }

        internal long GetChangesetId()
        {
            return mChangesetId;
        }

        internal void ChangeTree(Node tree)
        {
            lock (this)
            {
                mTree = tree;
            }
        }

        internal void CleanChangesTree()
        {
            mChangesTree = null;
        }

        internal void InitializeChangesTree(TreeChangedNode changesTree)
        {
            mChangesTree = changesTree;
        }

        Node mTree;
        TreeChangedNode mChangesTree;
        long mChangesetId;
    }
}
