using System;
using System.Collections.Generic;
using Codice.Client.Commands.Mount;
using Codice.Client.Commands.Xlinks;
using Codice.CM.Common.Tree;
using PlasticDrive.Writable.Tree;

namespace PlasticDrive.Writable.WkTree
{
    static class MergeWorkspaceTree
    {
        // NEW TREE IS CHANGED IN THIS OPERATION
        // I decided to modify it instead of cloning nodes.
        internal static Node MergeNewTreeWithPrivates(
            DateTime now, Node newTree, Node oldTree)
        {
            PrivateItemsByMountIdCache directoriesWithPrivateCache =
                new PrivateItemsByMountIdCache();

            if (oldTree.GetChildrenCount() == 0)
                return newTree;

            IndexControlledDirectoriesWithPrivateChildren(
                MountPointId.WORKSPACE_ROOT,
                string.Empty,
                oldTree,
                directoriesWithPrivateCache);

            CompleteNewTreeWithPrivates(
                MountPointId.WORKSPACE_ROOT,
                newTree,
                directoriesWithPrivateCache);

            if (directoriesWithPrivateCache.IsEmpty())
            {
                //nothing more to do
                // all privates were reallocated.
                return newTree;
            }

            CompleteUnlinkedPrivateItems(
                now, newTree, directoriesWithPrivateCache.GetAll());

            return newTree;
        }

        static void CompleteNewTreeWithPrivates(
            MountPointId mountId, Node newNode, PrivateItemsByMountIdCache cache)
        {
            if(newNode.GetChildrenCount() > 0)
            {
                foreach(Node child in newNode.GetChildren())
                {
                    if (!child.IsDirectory())
                        continue;

                    CompleteNewTreeWithPrivates(
                        GetChildMountId(child, mountId), child, cache);
                }
            }

            NodeWithPrivates privates = cache.Pop(mountId, GetItemId(newNode));
            if (privates == null)
                return;

            AddPrivateItems(newNode, privates.PrivateChildren);
        }

        static void AddPrivateItems(Node node, List<Node> privateItems)
        {
            HashSet<string> newChildNames = HashDirectoryEntries(node.GetChildren());

            foreach (Node child in privateItems)
            {
                string childName = child.GetName();

                int i = 0;
                while (newChildNames.Contains(childName))
                    childName = child.GetName() + ".private." + i++;

                node.AddChild(child, childName);
                newChildNames.Add(childName);
            }
        }

        static HashSet<string> HashDirectoryEntries(IEnumerable<Node> children)
        {
            HashSet<string> result = new HashSet<string>();
            if (children == null)
                return result;

            foreach (Node child in children)
                result.Add(child.GetName());

            return result;
        }

        static void IndexControlledDirectoriesWithPrivateChildren(
            MountPointId mountId,
            string path,
            Node node,
            PrivateItemsByMountIdCache directoriesWithPrivates)
        {
            NodeWithPrivates nodeWithPrivates = null;
            foreach(Node child in node.GetChildren())
            {
                if (!child.IsControlled())
                {
                    nodeWithPrivates = new NodeWithPrivates();
                    nodeWithPrivates.Path = path;
                    nodeWithPrivates.PrivateChildren.Add(child);
                    continue;
                }

                if (child.GetChildrenCount() == 0)
                    continue;

                IndexControlledDirectoriesWithPrivateChildren(
                    GetChildMountId(child, mountId),
                    path + "/" + child.GetName(),
                    child,
                    directoriesWithPrivates);
            }

            if (nodeWithPrivates == null)
                return;

            directoriesWithPrivates.Add(mountId, GetItemId(node), nodeWithPrivates);
        }

        static void CompleteUnlinkedPrivateItems(
            DateTime now,
            Node newTree,
            List<NodeWithPrivates> privates)
        {
            foreach(NodeWithPrivates nodeWithPrivates in privates)
            {
                Node parent = GetOrCreateNode(now, nodeWithPrivates.Path, newTree);
                AddPrivateItems(parent, nodeWithPrivates.PrivateChildren);
            }
        }

        static Node GetOrCreateNode(DateTime now, string path, Node newTree)
        {
            string[] paths = path.Split('/');
            Node node = newTree;
            for (int i = 1; i < paths.Length; i++)
            {
                string name = paths[i];
                Node child = WalkTree.GetChildByName(node.GetChildren(), name);
                if (child == null)
                {
                    child = new Node();
                    child.InitPrivate(now, name, System.IO.FileAttributes.Directory);
                    node.AddChild(child, name);
                }
                node = child;
            }
            return node;
        }

        static object GetItemId(Node node)
        {
            Xlink xlink = node.GetXlink();
            if (xlink != null)
                return xlink.GUID;
            return node.GetItemId();
        }

        static MountPointId GetChildMountId(Node child, MountPointId parentId)
        {
            ClientXlink xlink = child.GetXlink();
            return xlink == null ? parentId : MountPointId.BuildForXlink(xlink.GUID, parentId);
        }

        class NodeWithPrivates
        {
            internal string Path;
            internal List<Node> PrivateChildren = new List<Node>();
        }

        class PrivateItemsByMountIdCache
        {
            internal void Add(MountPointId mountId, object itemId, NodeWithPrivates node)
            {
                Dictionary<object, NodeWithPrivates> itemCache;
                if (!mCache.TryGetValue(mountId, out itemCache))
                {
                    itemCache = new Dictionary<object, NodeWithPrivates>();
                    mCache.Add(mountId, itemCache);
                }
                itemCache.Add(itemId, node);
            }

            internal NodeWithPrivates Pop(MountPointId mountId, object itemId)
            {
                Dictionary<object, NodeWithPrivates> itemCache;
                if (!mCache.TryGetValue(mountId, out itemCache))
                    return null;

                NodeWithPrivates result;
                if (!itemCache.TryGetValue(itemId, out result))
                    result = null;

                itemCache.Remove(itemId);
                if(itemCache.Count == 0)
                    mCache.Remove(mountId);

                return result;
            }

            internal bool IsEmpty()
            {
                return mCache.Count == 0;
            }

            internal List<NodeWithPrivates> GetAll()
            {
                List<NodeWithPrivates> result = new List<NodeWithPrivates>();
                foreach (Dictionary<object, NodeWithPrivates> cache in mCache.Values)
                    result.AddRange(cache.Values);

                return result;
            }

            Dictionary<MountPointId, Dictionary<object, NodeWithPrivates>> mCache =
                new Dictionary<MountPointId, Dictionary<object, NodeWithPrivates>>();
        }
    }
}
