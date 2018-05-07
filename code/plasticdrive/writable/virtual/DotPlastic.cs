using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PlasticDrive.Writable.Tree;

namespace PlasticDrive.Writable.Virtual
{
    class DotPlastic
    {
        static internal Node Add(Node root, string dotPlasticName, DateTime now)
        {
            Node dotPlastic = root.CreateChild();

            dotPlastic.InitPrivate(
                now,
                dotPlasticName,
                FileAttributes.Directory | FileAttributes.Hidden);

            return dotPlastic;
        }

        static internal Node AddPlasticWkTree(Node parent, DateTime now)
        {
            Node result = parent.CreateChild();

            result.InitPrivate(
                now,
                "plastic.wktree",
                FileAttributes.Normal);

            return result;
        }

        static internal Node AddPlasticWkChangesTree(Node parent, DateTime now)
        {
            Node result = parent.CreateChild();

            result.InitPrivate(
                now,
                "plastic.changes",
                FileAttributes.Normal);

            return result;
        }

        static internal Node AddPlasticWorkspace(Node parent, DateTime now)
        {
            Node result = parent.CreateChild();

            result.InitPrivate(
                now,
                "plastic.workspace",
                FileAttributes.Normal);

            return result;
        }

        static internal Node AddPlasticSelector(Node parent, DateTime now)
        {
            Node result = parent.CreateChild();

            result.InitPrivate(
                now,
                "plastic.selector",
                FileAttributes.Normal);

            return result;
        }
    }
}
