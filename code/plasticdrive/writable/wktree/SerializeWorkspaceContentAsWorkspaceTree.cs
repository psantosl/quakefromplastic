using System;
using System.Collections.Generic;
using System.IO;

using Codice.Client.Commands.TransformerRule;
using Codice.Client.Commands.Xlinks;
using Codice.CM.Common;
using Codice.CM.Common.Serialization;
using Codice.CM.WorkspaceServer;
using Codice.CM.WorkspaceServer.DataStore;
using PlasticDrive.Writable.Tree;

namespace PlasticDrive.Writable.WkTree
{
    static class SerializeWorkspaceContentAsWorkspaceTree
    {
        internal static void Serialize(Stream stream, WorkspaceContent content)
        {
            int ini = Environment.TickCount;

            Node tree = content.GetTree();

            if (content == null || tree == null)
                return;

            PlasticBinaryWriter writer = new PlasticBinaryWriter(stream);

            writer.WriteString(WorkspaceTreeDataStore.TREE_HEADER);
            writer.WriteByte(TreeVersionManager.CURRENT_VERSION);

            List<RepositorySpec> repSpecs = new List<RepositorySpec>();
            List<SEID> seids = new List<SEID>();

            LookForSeidsAndRepSpecs(tree, repSpecs, seids);

            writer.WriteInt64(content.GetChangesetId());

            RepSpecDataStore.WriteRepSpecs(writer, repSpecs);

            SeidDataStore.WriteSeids(writer, seids);

            WriteNode(writer, tree, repSpecs, seids);

            TransformedRulesDataStore.WriteRules(
                writer, new List<ExtendedTransformerRule>());

            writer.Flush();
        }

        static void WriteNode(
            PlasticBinaryWriter writer,
            Node node,
            List<RepositorySpec> repSpecs,
            List<SEID> seids)
        {
            NodeType type = GetNodeTypeFromInstanceType(node);
            string wkNodeName = StorableNodeTranslator.GetStorableName(node.GetName(), type);

            // write the node
            writer.WriteString(wkNodeName);

            WorkspaceTreeDataStore.WriteFileLocalInfo(writer, null, TreeVersionOptions);

            writer.WriteInt16((short)repSpecs.IndexOf(node.GetRepSpec()));

            Node.SerializeNode.SerializeRevInfo(writer, node, seids);

            if (node.GetXlink() != null)
            {
                ClientXlink xlink = node.GetXlink();
                WorkspaceTreeDataStore.WriteXlink(writer, repSpecs, xlink, TreeVersionOptions);
            }

            // write children
            int childrenCount = GetControlledChildrenCount(node);
            writer.WriteUInt32((uint)childrenCount);

            if (childrenCount == 0)
                return;

            foreach (Node child in node.GetChildren())
            {
                if (!child.IsControlled())
                    continue;

                WriteNode(writer, child, repSpecs, seids);
            }
        }

        internal static void LookForSeidsAndRepSpecs(
            Node tree,
            List<RepositorySpec> repSpecs,
            List<SEID> seids)
        {
            Stack<Node> nodes = new Stack<Node>();
            nodes.Push(tree);

            while(nodes.Count > 0)
            {
                Node node = nodes.Pop();

                RepositorySpec repSpec = node.GetRepSpec();
                if(!repSpecs.Contains(repSpec))
                    repSpecs.Add(repSpec);

                ClientXlink xlink = node.GetXlink();
                if(xlink != null && !repSpecs.Contains(xlink.RepSpec))
                        repSpecs.Add(xlink.RepSpec);

                SEID seid = node.GetOwner();
                if (seid != null && !seids.Contains(seid))
                    seids.Add(seid);

                if (node.GetChildren() == null)
                    continue;

                foreach (Node child in node.GetChildren())
                {
                    if (!child.IsControlled())
                        continue;

                    nodes.Push(child);
                }
            }
        }

        static int GetControlledChildrenCount(Node node)
        {
            if (node.GetChildrenCount() == 0)
                return 0;

            int result = 0;
            foreach (Node child in node.GetChildren())
                if (child.IsControlled())
                    result++;
            return result;
        }

        static NodeType GetNodeTypeFromInstanceType(Node node)
        {
            ClientXlink xlink = node.GetXlink();
            if (xlink == null)
                return NodeType.Normal;

            if (xlink.Writable)
                return NodeType.WritableXlink;

            return NodeType.ReadonlyXlink;
        }

        internal static readonly TreeVersionManager.VersionOptions TreeVersionOptions =
            TreeVersionManager.GetVersionOptions(TreeVersionManager.CURRENT_VERSION);
    }
}
