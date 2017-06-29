using System;
using System.Collections.Generic;
using System.IO;

using Codice.CM.Common;

namespace PlasticDrive.Writable.Tree
{
    static class WalkTree
    {
        internal static Node Find(Node root, string path)
        {
            if (root == null)
                return null;

            string[] paths = path.Split(
                PATH_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);

            int start = 0;

            if (path.IndexOf(':') >= 0)
                start = 1;

            Node node = root;
            for (int i = start; i < paths.Length; i++)
            {
                node = GetChildByName(node.GetChildren(), paths[i]);
                if (node == null)
                    return null;
            }

            return node;
        }

        internal static ulong GetTotalSize(Node root)
        {
            if (root == null)
                return 0;

            long totalSize = 0;

            Queue<Node> nodes = new Queue<Node>();
            nodes.Enqueue(root);
            while(nodes.Count > 0)
            {
                Node node = nodes.Dequeue();

                totalSize += node.GetSize();

                if (node.GetChildrenCount() == 0)
                    continue;

                foreach (Node child in node.GetChildren())
                    nodes.Enqueue(child);
            }

            return (ulong)totalSize;
        }

        internal static Node GetChildByName(IEnumerable<Node> children, string childName)
        {
            if(children == null)
                return null;

            foreach (Node child in children)
            {
                // if we port this code to mac or linux
                // the string comparison should be different.
                if (string.Compare(child.GetName(), childName,
                        StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return child;
                }
            }
            return null;
        }

        readonly static char[] PATH_SEPARATOR = new char[] { Path.DirectorySeparatorChar };
    }
}
