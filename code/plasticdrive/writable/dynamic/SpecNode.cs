using System.IO;

using Dokan;

using Codice.CM.Common;
using Codice.Client.Common;

using PlasticDrive.Writable.Tree;

namespace PlasticDrive.Writable.Dynamic
{
    class SpecNode
    {
        internal static int CreateFile(
            string fileName,
            FileAccess access,
            FileShare share,
            FileMode mode,
            FileOptions options,
            Node tree,
            FileCache fileCache,
            FileHandles fileHandles,
            PlasticAPI api)
        {
            if (fileName.IndexOf('#') < 0)
                return -1;

            if (access == FileAccess.ReadWrite || access == FileAccess.Write)
                return -1;

            Node node = GetNode(fileName, tree);

            if (node == null)
                return -1;

            RevisionInfo revInfo = GetRevisionInfo(fileName, node, api);

            if (revInfo == null)
                return -1;

            string fileToOpen = fileCache.GetFile(
                node.GetRepSpec(),
                revInfo,
                fileName,
                api);

            // and this controls the opened handle
            int handle = fileHandles.OpenFile(
                fileToOpen,
                fileName,
                access, share, mode, options);

            return handle;
        }

        internal static int GetFileInformation(
            string fileName,
            FileInformation fileinfo,
            Node tree,
            PlasticAPI api)
        {
            if (fileName.IndexOf('#') < 0)
                return -1;

            Node node = GetNode(fileName, tree);

            if (node == null)
                return -1;

            RevisionInfo revInfo = GetRevisionInfo(fileName, node, api);

            if (revInfo == null)
                return -1;

            fileinfo.FileName = Path.GetFileName(fileName);
            fileinfo.Attributes = FileAttributes.Normal;
            fileinfo.CreationTime = revInfo.LocalTimeStamp;
            fileinfo.LastAccessTime = revInfo.LocalTimeStamp;
            fileinfo.LastWriteTime = revInfo.LocalTimeStamp;
            fileinfo.Length = revInfo.Size;

            return 0;
        }

        static Node GetNode(string fileName, Node tree)
        {
            string path = fileName.Substring(0, fileName.IndexOf('#'));

            return WalkTree.Find(tree, path);
        }

        static RevisionInfo GetRevisionInfo(
            string fileName, Node node, PlasticAPI api)
        {
            string stringSpec = fileName.Substring(fileName.LastIndexOf(Path.DirectorySeparatorChar) + 1).Replace('_', ':');

            PathUserRevSpec spec = api.GetPathRevSpec(stringSpec);

            if (spec == null)
                return null;

            RevSpec revSpec = new RevSpec();
            revSpec.itemId = node.GetItemId();
            revSpec.changesetNumber = long.Parse(spec.csNumber);
            revSpec.brName = spec.brName;
            revSpec.lbName = spec.lbName;
            revSpec.repSpec = node.GetRepSpec();

            return api.GetRevInfoFromRevSpec(node.GetRepSpec(), revSpec);
        }
    }
}
