using System;
using System.Collections.Generic;
using System.IO;

using Codice.CM.Common;
using Codice.CM.Common.Serialization;
using Codice.CM.WorkspaceServer;

namespace PlasticDrive.Writable.Tree
{
    partial class Node
    {
        internal static class SerializeNode
        {
            public enum NodeType : byte
            {
                Private = 0,
                Controlled = 1,
                Xlink = 2
            };

            internal static NodeType GetNodeType(Node node)
            {
                if (node.mControlled == null)
                    return NodeType.Private;

                return node.GetXlink() != null ? NodeType.Xlink : NodeType.Controlled;
            }

            internal static void Serialize(
                PlasticBinaryWriter writer,
                Node node,
                List<RepositorySpec> repSpecs,
                List<SEID> seids)
            {
                NodeType nodeType = GetNodeType(node);
                writer.WriteByte((byte)nodeType);
                writer.WriteString(node.mName);
                writer.WriteUInt32(node.mNodeId);
                writer.WriteInt64(node.mLocalSize);
                writer.WriteUInt16((ushort)node.mLocalAttributes);
                writer.WriteUInt64((ulong)node.mLocalCreationTime.Ticks);
                writer.WriteUInt64((ulong)node.mLocalLastAccessTime.Ticks);
                writer.WriteUInt64((ulong)node.mLocalLastWriteTime.Ticks);

                if (nodeType != SerializeNode.NodeType.Private)
                {
                    Controlled.SerializeControlled.Serialize(
                        writer, node.mControlled, repSpecs, seids, nodeType);
                }

                writer.WriteUInt32((uint)node.GetChildrenCount());

                if (node.GetChildrenCount() == 0)
                    return;

                foreach (Node child in node.GetChildren())
                    Serialize(writer, child, repSpecs, seids);
            }

            internal static void SerializeRevInfo(
                PlasticBinaryWriter writer, Node node, List<SEID> seids)
            {
                Node.Controlled.SerializeControlled.SerializeRevInfo(
                    writer, node.mControlled, seids);
            }
        }

        internal static class DeserializeNode
        {
            internal static Node Deserialize(
                PlasticBinaryReader reader,
                List<RepositorySpec> repSpecs,
                List<SEID> seids)
            {
                Node node = new Node();
                SerializeNode.NodeType nodeType = (SerializeNode.NodeType)reader.ReadByte();
                node.mName = reader.ReadString();
                node.mNodeId = reader.ReadUInt32();
                node.mLocalSize = reader.ReadInt64();
                node.mLocalAttributes = (FileAttributes)reader.ReadUInt16();
                node.mLocalCreationTime = new DateTime((long)reader.ReadUInt64());
                node.mLocalLastAccessTime = new DateTime((long)reader.ReadUInt64());
                node.mLocalLastWriteTime = new DateTime((long)reader.ReadUInt64());

                if (nodeType != SerializeNode.NodeType.Private)
                {
                    node.mControlled = Controlled.DeserializeControlled.Deserialize(
                        reader, repSpecs, seids, nodeType);
                }

                uint childrenCount = reader.ReadUInt32();

                if (childrenCount == 0)
                    return node;

                node.mChildren = new List<Node>();

                for (int i = 0; i < childrenCount; i++)
                    node.mChildren.Add(Deserialize(reader, repSpecs, seids));

                return node;
            }
        }

        partial class Controlled
        {
            internal static class SerializeControlled
            {
                internal static void Serialize(
                    PlasticBinaryWriter writer,
                    Controlled controlled,
                    List<RepositorySpec> repSpecs,
                    List<SEID> seids,
                    SerializeNode.NodeType nodeType)
                {
                    writer.WriteUInt16((ushort)repSpecs.IndexOf(controlled.mRepSpec));

                    SerializeRevInfo(writer, controlled, seids);

                    if (nodeType != SerializeNode.NodeType.Xlink)
                        return;

                    WorkspaceTreeDataStore.WriteXlink(writer, repSpecs, controlled.mXlink,
                        WkTree.SerializeWorkspaceContentAsWorkspaceTree.TreeVersionOptions);
                }

                internal static void SerializeRevInfo(
                    PlasticBinaryWriter writer, Controlled controlled, List<SEID> seids)
                {
                    RevisionInfoDataStore.WriteRevisionInfo(
                        writer, controlled.mRevInfo, seids,
                        WkTree.SerializeWorkspaceContentAsWorkspaceTree.TreeVersionOptions);
                }
            }

            internal static class DeserializeControlled
            {
                internal static Controlled Deserialize(
                    PlasticBinaryReader reader,
                    List<RepositorySpec> repSpecs,
                    List<SEID> seids,
                    SerializeNode.NodeType nodeType)
                {
                    Controlled controlled = new Controlled();
                    controlled.mRepSpec = repSpecs[reader.ReadUInt16()];
                    controlled.mRevInfo = RevisionInfoDataStore.ReadRevisionInfo(
                        reader, null, controlled.mRepSpec, seids,
                        WkTree.SerializeWorkspaceContentAsWorkspaceTree.TreeVersionOptions);

                    if (nodeType != SerializeNode.NodeType.Xlink)
                        return controlled;

                    controlled.mXlink = WorkspaceTreeDataStore.ReadXlink(reader, repSpecs,
                        WkTree.SerializeWorkspaceContentAsWorkspaceTree.TreeVersionOptions);

                    return controlled;
                }
            }
        }
    }
}