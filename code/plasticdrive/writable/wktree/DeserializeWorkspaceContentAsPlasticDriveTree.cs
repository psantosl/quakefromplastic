using System;
using System.Collections.Generic;
using System.IO;

using Codice.Client.Commands.Xlinks;
using Codice.CM.Common;
using Codice.CM.Common.Serialization;
using Codice.CM.WorkspaceServer;
using Codice.CM.WorkspaceServer.DataStore;
using PlasticDrive.Writable.Tree;

namespace PlasticDrive.Writable.WkTree
{
    static class DeserializeWorkspaceContentAsPlasticDriveTree
    {
        internal static WorkspaceContent Deserialize(DateTime now, Stream stream)
        {
            int ini = Environment.TickCount;

            PlasticBinaryReader reader = new PlasticBinaryReader(stream);

            string header = reader.ReadString();
            if (header != WorkspaceTreeDataStore.TREE_HEADER)
                return null;

            byte version = reader.ReadByte();
            if (!TreeVersionManager.IsValidTreeVersion(version))
                return null;

            long changesetId = reader.ReadInt64();

            List<RepositorySpec> repSpecs = RepSpecDataStore.ReadRepSpecs(reader);
            List<SEID> seids = SeidDataStore.ReadSeids(reader);

            Node tree = ReadNode(now, reader, repSpecs, seids);

            TransformedRulesDataStore.ReadRules(reader, false);

            return new WorkspaceContent(tree, changesetId);
        }

        static Node ReadNode(
            DateTime now,
            PlasticBinaryReader reader,
            List<RepositorySpec> repSpecs,
            List<SEID> seids)
        {
            string name = reader.ReadString();

            WorkspaceTreeDataStore.ReadFilelocalInfo(reader, TreeVersionOptions);

            RepositorySpec repSpec = repSpecs[reader.ReadInt16()];

            RevisionInfo revInfo = RevisionInfoDataStore.ReadRevisionInfo(
                reader, null, repSpec, seids, TreeVersionOptions);

            Node node = BuildNode(now, reader, name, repSpec, repSpecs, revInfo);

            uint children = reader.ReadUInt32();

            // write children
            if (children == 0)
                return node;

            for (int i = 0; i < children; ++i)
            {
                Node child = ReadNode(now, reader, repSpecs, seids);
                node.AddChild(child, child.GetName());
            }

            return node;
        }

        static Node BuildNode(
            DateTime now,
            PlasticBinaryReader reader,
            string name,
            RepositorySpec repSpec,
            List<RepositorySpec> repSpecs,
            RevisionInfo revInfo)
        {
            NodeType type = StorableNodeTranslator.GetNodeTypeFromName(name);
            name = StorableNodeTranslator.GetCleanName(name, type);

            ClientXlink xlink = null;
            switch (type)
            {
                case NodeType.Normal:
                    break;

                case NodeType.ReadonlyXlink:
                case NodeType.WritableXlink:
                    xlink = WorkspaceTreeDataStore.ReadXlink(
                        reader, repSpecs, TreeVersionOptions);
                    xlink.Writable = type == NodeType.WritableXlink;
                    break;

                default:
                    throw new NotSupportedException(
                        string.Format("Not supported NodeType {0}", type));
            }

            FileAttributes attributes =
                xlink != null || revInfo.Type == EnumRevisionType.enDirectory ?
                    FileAttributes.Directory : FileAttributes.Normal;

            Node result = new Node();
            result.InitControlled(
                now, name, attributes, revInfo.Size, repSpec, revInfo, xlink);

            return result;
        }

        internal static readonly TreeVersionManager.VersionOptions TreeVersionOptions =
            TreeVersionManager.GetVersionOptions(TreeVersionManager.CURRENT_VERSION);
    }
}
