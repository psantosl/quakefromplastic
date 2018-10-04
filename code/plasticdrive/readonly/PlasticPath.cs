using System;
using System.Collections.Generic;
using System.Text;

using Codice.Client.Common;
using Codice.Client.Commands;

using Codice.CM.Common;
using Codice.CM.Common.Serialization;
using Codice.CM.Common.Selectors;
using Codice.CM.Common.Tree;

namespace PlasticDrive.Readonly
{
    class PlasticPath
    {
        internal static string ToCMFormat(string path)
        {
            // change
            return WorkspacePath.ToCMFormat(path);
        }

        internal static string GetParentDirectory(string cmPath)
        {
            return WorkspacePath.GetParentDirectory(cmPath);
        }

        internal static string GetName(string cmPath)
        {
            return WorkspacePath.GetName(GetParentDirectory(cmPath), cmPath);
        }

        internal static string ConcatPath(string basePath, string cmPath)
        {
            return WorkspacePath.GetWorkspacePathFromCmPath(basePath, cmPath, '\\');
        }

        internal static string CombineCmPath(string cmpath, string name)
        {
            return WorkspacePath.CombineCmPath(cmpath, name);
        }
    }
}
