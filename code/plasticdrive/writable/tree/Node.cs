using System;
using System.Collections.Generic;
using System.IO;

using Codice.CM.Common;
using Codice.Client.Commands.Xlinks;

namespace PlasticDrive.Writable.Tree
{
    partial class Node
    {
        string mName;

        uint mNodeId;
        // OS file Information
        protected FileAttributes mLocalAttributes;
        protected DateTime mLocalCreationTime;
        protected DateTime mLocalLastAccessTime;
        protected DateTime mLocalLastWriteTime;
        protected long mLocalSize;

        // Local hash?
        // string mLocalHash;

        Controlled mControlled;

        List<Node> mChildren;

        internal Node CreateChild()
        {
            Node result = new Node();
            result.mNodeId = NodeId.Next();

            if (mChildren == null)
                mChildren = new List<Node>();

            mChildren.Add(result);

            return result;
        }

        internal bool ExistChild(Node child)
        {
            if (mChildren == null)
                return false;

            return mChildren.IndexOf(child) != -1;
        }

        internal bool DeleteChild(Node child)
        {
            return mChildren.Remove(child);
        }

        internal void AddChild(Node child, string newName)
        {
            if (mChildren == null)
                mChildren = new List<Node>();

            child.mName = newName;

            mChildren.Add(child);
        }

        internal void InitControlled(
            DateTime now,
            string name,
            FileAttributes attr,
            long size,
            RepositorySpec repSpec,
            RevisionInfo revInfo,
            ClientXlink xlink)
        {
            mName = name;
            mLocalAttributes = attr;
            mLocalCreationTime = now;
            mLocalLastAccessTime = now;
            mLocalLastWriteTime = now;
            mLocalSize = size;

            mControlled = Controlled.Create(repSpec, revInfo, xlink);
        }

        internal void InitPrivate(
            DateTime now,
            string name,
            FileAttributes attr)
        {
            mName = name;
            mLocalAttributes = attr;
            mLocalCreationTime = now;
            mLocalCreationTime = now;
            mLocalLastAccessTime = now;
            mLocalLastWriteTime = now;
            mLocalSize = 0;
        }

        internal void Checkout(long coBrId, SEID coOwner)
        {
            if (mControlled == null)
                return;

            mControlled.Checkout(coBrId, coOwner);
        }

        internal bool IsDirectory()
        {
            return (mLocalAttributes & FileAttributes.Directory) == FileAttributes.Directory;
        }

        internal bool IsControlled()
        {
            return mControlled != null;
        }

        internal bool IsCheckedOut()
        {
            return mControlled != null && mControlled.IsCheckedout();
        }

        internal bool IsControlledAndReadOnly()
        {
            return mControlled != null && (!mControlled.IsCheckedout());
        }

        internal void UpdateSize(long size)
        {
            mLocalSize = size;
        }

        internal IEnumerable<Node> GetChildren()
        {
            return mChildren;
        }

        internal int GetChildrenCount()
        {
            return mChildren == null ? 0 : mChildren.Count;
        }

        internal string GetName()
        {
            return mName;
        }

        internal uint GetNodeId()
        {
            return mNodeId;
        }

        internal RepositorySpec GetRepSpec()
        {
            return mControlled == null ? null : mControlled.GetRepSpec();
        }

        internal ClientXlink GetXlink()
        {
            return mControlled == null ? null : mControlled.GetXlink();
        }

        internal SEID GetOwner()
        {
            return mControlled == null ? null : mControlled.GetOwner();
        }

        internal long GetSize()
        {
            return mLocalSize;
        }

        internal long GetItemId()
        {
            return mControlled == null ? -1 : mControlled.GetItemId();
        }

        internal long GetBranchId()
        {
            return mControlled == null ? -1 : mControlled.GetBranchId();
        }

        internal RevisionInfo CloneRevisionInfo()
        {
            return mControlled == null ? null : mControlled.CloneRevisionInfo();
        }

        partial class Controlled
        {
            internal void Checkout(long coBrId, SEID coOwner)
            {
                mRevInfo.ParentId = mRevInfo.Id;
                mRevInfo.Id = -1;
                mRevInfo.BranchId = coBrId;
                mRevInfo.Owner = coOwner;
                mRevInfo.CheckedOut = true;
            }

            internal RepositorySpec GetRepSpec()
            {
                return mRepSpec;
            }

            internal ClientXlink GetXlink()
            {
                return mXlink;
            }

            internal bool IsCheckedout()
            {
                return mRevInfo.CheckedOut;
            }

            internal SEID GetOwner()
            {
                return mRevInfo.Owner;
            }

            internal long GetSize()
            {
                return mRevInfo.Size;
            }

            internal long GetBranchId()
            {
                return mRevInfo.BranchId;
            }

            internal long GetItemId()
            {
                return mRevInfo.ItemId;
            }

            internal RevisionInfo CloneRevisionInfo()
            {
                return RevisionInfoCloner.CloneRevision(mRevInfo);
            }

            internal static Controlled Create(
                RepositorySpec repSpec,
                RevisionInfo revInfo,
                ClientXlink xlink)
            {
                Controlled result = new Controlled();
                result.mRepSpec = repSpec;
                result.mRevInfo = revInfo;
                result.mXlink = xlink;

                return result;
            }

            RevisionInfo mRevInfo;
            RepositorySpec mRepSpec;
            ClientXlink mXlink;
        }
    }
}
