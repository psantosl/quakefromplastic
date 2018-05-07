using System.Collections.Generic;
using System.IO;

using Codice.CM.Common;
using Codice.CM.Common.Serialization;
using Codice.CM.WorkspaceServer;
using PlasticDrive.Writable.WkTree;

namespace PlasticDrive.Writable.Tree
{
    static class SerializeWorkspaceContent
    {
        internal static void Serialize(Stream stream, WorkspaceContent content)
        {
            PlasticBinaryWriter writer = new PlasticBinaryWriter(stream);

            writer.WriteByte(CURRENT_VERSION);

            writer.WriteInt64(content.GetChangesetId());

            List<RepositorySpec> repSpecs = new List<RepositorySpec>();
            List<SEID> seids = new List<SEID>();

            SerializeWorkspaceContentAsWorkspaceTree.LookForSeidsAndRepSpecs(
                content.GetTree(), repSpecs, seids);

            RepSpecDataStore.WriteRepSpecs(writer, repSpecs);
            SeidDataStore.WriteSeids(writer, seids);

            Node.SerializeNode.Serialize(writer, content.GetTree(), repSpecs, seids);
        }

        internal const byte CURRENT_VERSION = 1;
    }

    static class DeserializeWorkspaceContent
    {
        internal static WorkspaceContent Deserialize(Stream stream)
        {
            PlasticBinaryReader reader = new PlasticBinaryReader(stream);

            if (reader.ReadByte() != SerializeWorkspaceContent.CURRENT_VERSION)
                return null;

            long changesetId = reader.ReadInt64();

            List<RepositorySpec> repSpecs = RepSpecDataStore.ReadRepSpecs(reader);
            List<SEID> seids = SeidDataStore.ReadSeids(reader);

            Node tree = Node.DeserializeNode.Deserialize(reader, repSpecs, seids);

            return new WorkspaceContent(tree, changesetId);
        }
    }
}