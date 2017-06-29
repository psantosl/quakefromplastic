using System;
using System.Collections;
using System.IO;

using Dokan;

using Codice.CM.Common;
using Codice.Client.Commands;
using Codice.CM.Common.Tree;

using PlasticDrive;

namespace PlasticDrive.Readonly
{
    class SelectorTree
    {
        TreeContent mFileSystemContent = null;
        string mMountPoint;
        PlasticAPI mPlasticAPI;
        FileCache mCache;

        internal SelectorTree(
            string mountPoint,
            string cachePath,
            PlasticAPI plasticAPI)
        {
            mMountPoint = mountPoint;
            mPlasticAPI = plasticAPI;
            mCache = new FileCache(cachePath);
        }

        internal string GetPath(string filename)
        {
            if (filename.ToLower() == mMountPoint.ToLower())
                return InternalNames.RootDir;

            return PlasticPath.ToCMFormat(filename);
        }

        internal bool Exists(string cmPath)
        {
            return GetItem(cmPath) != null;
        }

        internal bool DirectoryExists(string cmPath)
        {
            TreeNode file = GetItem(cmPath);
            if (file == null)
                return false;
            return file.RevInfo.Type == EnumRevisionType.enDirectory;
        }

        internal void FillFileInformation(string path, FileInformation fileinfo)
        {
            TreeNode item = GetItem(path);

            if (item == null)
                return;

            if (item.RevInfo.Type == EnumRevisionType.enDirectory)
            {
                fileinfo.Attributes = FileAttributes.ReadOnly | FileAttributes.Directory;
                fileinfo.CreationTime = item.RevInfo.LocalTimeStamp;
                fileinfo.LastAccessTime = item.RevInfo.LocalTimeStamp;
                fileinfo.LastWriteTime = item.RevInfo.LocalTimeStamp;
                fileinfo.Length = 0;// f.Length;
            }
            else
            {
                fileinfo.Attributes = FileAttributes.ReadOnly;
                fileinfo.CreationTime = item.RevInfo.LocalTimeStamp;
                fileinfo.LastAccessTime = item.RevInfo.LocalTimeStamp;
                fileinfo.LastWriteTime = item.RevInfo.LocalTimeStamp;
                fileinfo.Length = item.RevInfo.Size;
            }
        }

        internal void FindFiles(string path, ArrayList files)
        {
            TreeNode dir = GetItem(path);

            if (dir == null)
                return;

            if (!dir.HasChildren)
                return;

            foreach (TreeNode child in dir.Children)
            {
                FileInformation fi = new FileInformation();
                fi.Attributes = FileAttributes.ReadOnly;

                bool bDir = (child.RevInfo.Type == EnumRevisionType.enDirectory);

                if (bDir)
                    fi.Attributes |= FileAttributes.Directory;

                fi.CreationTime = child.RevInfo.LocalTimeStamp;
                fi.LastAccessTime = child.RevInfo.LocalTimeStamp;
                fi.LastWriteTime = child.RevInfo.LocalTimeStamp;
                fi.Length = bDir ? 0 : child.RevInfo.Size;
                fi.FileName = child.Name;
                files.Add(fi);
            }
        }

        internal bool FileExists(string cmPath)
        {
            TreeNode file = GetItem(cmPath);

            if (file == null)
                return false;

            Console.WriteLine(FileCache.HashToHex(file.RevInfo.Hash));

            return file.RevInfo.Type != EnumRevisionType.enDirectory;
        }

        internal string GetFile(string path)
        {
            TreeNode item = GetItem(path);

            if (item == null)
                return string.Empty;

            if ( item.RevInfo.Type != EnumRevisionType.enDirectory )
            {
                RepositorySpec repSpec = GetFS().GetRepositoryInfo(path).GetRepSpec();

                return mCache.GetFile(repSpec, item.RevInfo, path, mPlasticAPI);
            }

            return string.Empty;
        }

        void CleanTree()
        {
            mFileSystemContent = null;
        }

        ulong mFileSystemSize = 0;
        internal ulong GetTotalBytes()
        {
            if (mFileSystemSize != 0)
                return mFileSystemSize;

            return GetTreeSize(GetFS().Tree);
        }

        internal bool IsInitialized()
        {
            lock (mLock)
            {
                return mbIsInitialized;
            }
        }

        ulong GetTreeSize(TreeNode node)
        {
            if (node == null)
                return 0;

            if (!node.IsDirectory())
                return (ulong)node.RevInfo.Size;

            if (!node.HasChildren)
                return 0;

            ulong result = 0;

            foreach (TreeNode child in node.Children)
            {
                result += GetTreeSize(child);
            }

            mFileSystemSize = result;

            return result;
        }

        TreeNode GetItem(string cmPath)
        {
            TreeNode root = GetFS().Tree;

            if (root.Name == cmPath)
                return root;

            string[] levels = cmPath.Split('/');

            for (int i = 1; i < levels.Length; ++i)
            {
                root = GetChildren(root, levels[i]);

                if (root == null)
                    return null;
            }

            return root;
        }

        TreeNode GetChildren(TreeNode dir, string name)
        {
            if (dir.Children == null)
                return null;

            foreach (TreeNode child in dir.Children)
            {
                if ((child.Name == name) || (name == "*"))
                    return child;
            }
            return null;
        }

        TreeContent GetFS()
        {
            lock(mLock)
            {
                if (mbIsInitialized)
                    return mFileSystemContent;

                mFileSystemContent = mPlasticAPI.GetTreeContent(
                    InternalNames.RootDir, true);

                mbIsInitialized = true;

                return mFileSystemContent;
            }
        }

        bool mbIsInitialized = false;

        object mLock = new object();
    }
}
