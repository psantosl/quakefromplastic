using System;
using System.Collections;
using System.IO;

using Dokan;

using log4net;

using Codice.CM.Common;
using Codice.Client.Commands;
using Codice.CM.Common.Tree;

namespace PlasticDrive.Readonly
{
    class ControlledPath
    {
        internal ControlledPath(SelectorTree selectorTree, FileHandles handles)
        {
            mSelectorTree = selectorTree;
            mHandles = handles;
        }

        internal int CreateFile(
            string cmpath,
            bool bCreate,
            FileAccess access,
            FileShare share,
            FileMode mode,
            FileOptions options,
            DokanFileInfo info)
        {
            if (bCreate)
                return -1;

            if (mSelectorTree.DirectoryExists(cmpath))
            {
                DirectoryContext.Set(info);
                return 0;
            }

            // this downloads the temporary file
            string fileToOpen = mSelectorTree.GetFile(cmpath);

            // and this controls the opened handle
            int result = mHandles.OpenFile(
                fileToOpen,
                cmpath,
                access, share, mode, options);

            if (result == -1)
                return -1;

            info.Context = result;

            return 0;
        }

        SelectorTree mSelectorTree;
        FileHandles mHandles;
    }
}
