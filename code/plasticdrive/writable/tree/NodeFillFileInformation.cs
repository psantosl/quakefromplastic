using System.IO;
using Dokan;

namespace PlasticDrive.Writable.Tree
{
    partial class Node
    {
        internal void FillFileInformation(FileInformation fileinfo)
        {
            if (IsControlledAndReadOnly())
                fileinfo.Attributes |= FileAttributes.ReadOnly;

            fileinfo.FileName = mName;
            fileinfo.Attributes = mLocalAttributes;
            fileinfo.CreationTime = mLocalCreationTime;
            fileinfo.LastAccessTime = mLocalLastAccessTime;
            fileinfo.LastWriteTime = mLocalLastWriteTime;
            fileinfo.Length = mLocalSize;
        }

        internal void FillDates(FileInformation fileinfo)
        {
            fileinfo.CreationTime = mLocalCreationTime;
            fileinfo.LastAccessTime = mLocalLastAccessTime;
            fileinfo.LastWriteTime = mLocalLastWriteTime;
        }
    }
}
