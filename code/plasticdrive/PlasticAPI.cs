using System;

using Codice.Client.BaseCommands;
using Codice.Client.Common;
using Codice.Client.Commands;
using Codice.Client.Commands.WkTree;
using Codice.CM.Common;
using Codice.CM.WorkspaceServer;

namespace PlasticDrive
{
    internal class PlasticAPI
    {
        internal PlasticAPI(string clientconf)
        {
            mWkServer = null;
            mClientConfFile = clientconf;
        }

        internal void InitTreeSpec(string treespec)
        {
            Init();

            SpecGenerator spg = new SpecGenerator();

            ChangesetSpec csetSpec = spg.GenChangesetSpec(false, treespec);

            if (csetSpec == null)
            {
                InitFromMarkerSpec(treespec);
                return;
            }

            SpecResolver spr = new SpecResolver();

            mRepInfo = spr.GetRepInfo(csetSpec.repSpec);

            ChangesetInfo csetInfo = spr.GetObjectInfoFromSpec(csetSpec)
                as ChangesetInfo;

            if (csetInfo == null)
                throw new Exception(string.Format(
                    "Spec not found {0}", treespec));

            mChangesetId = csetInfo.ChangesetId;
        }

        internal TreeContent GetTreeContent(string cmPath, bool bRecurse)
        {
            Init();

            TreeContent treeContent = SelectorSolver.GetSelectorContent(
                mRepInfo, mChangesetId, cmPath, bRecurse);

            if (treeContent != null && treeContent.Tree != null)
                treeContent.Tree.Name = InternalNames.RootDir;

            return treeContent;
        }

        internal TreeContent GetSelectorContent(string selector)
        {
            Init();

            TreeContent result = SelectorHandler.Get().GetSelectorContent(
                selector, InternalNames.RootDir, SelectorFlags.Recursive);

            if (result != null && result.Tree != null)
                result.Tree.Name = InternalNames.RootDir;

            return result;
        }

        internal void GetFile(RepositorySpec repSpec, RevisionInfo revInfo, string output)
        {
            Init();
            GetBaseCommands().GetRevisionDataToFile(repSpec, revInfo, output);
        }

        internal string GetBranchName(RepositorySpec repSpec, long brId)
        {
            Init();

            RepositoryInfo repInfo = GetBaseCommands().GetRepositoryInfo(repSpec.Name, repSpec.Server);

            BranchInfo result = GetBaseCommands().GetBranch(brId, repInfo);

            if (result == null)
                return null;

            return result.Name;
        }

        internal PathUserRevSpec GetPathRevSpec(string stringSpec)
        {
            SpecGenerator gen = new SpecGenerator(null);

            gen.SolveWorkspaceAndRepository = false; // avoids loading wktree

            BaseRevSpec spec = gen.GenRevSpec(false, stringSpec);

            if (!(spec is PathUserRevSpec))
                return null;

            return spec as PathUserRevSpec;
        }

        internal RevisionInfo GetRevInfoFromRevSpec(RepositorySpec defaultRep, RevSpec spec)
        {
            SpecResolver resolver = new SpecResolver();
            RepositoryInfo repInfo = resolver.GetRepInfo(defaultRep);

            if (repInfo == null)
                return null;

            return resolver.GetObjectInfoFromSpec(spec) as RevisionInfo;
        }

        internal ItemHistory GetFileHistory(RepositorySpec repSpec, long itemId)
        {
            SpecResolver resolver = new SpecResolver();
            RepositoryInfo repInfo = resolver.GetRepInfo(repSpec);

            return GetBaseCommands().GetRevisionHistory(repInfo, itemId, false);
        }

/*        internal WorkspaceTreeNode GetWorkspaceTree(string path)
        {
            Init();
            IBaseCommands cmd = GetBaseCommands();

            WorkspaceInfo wkInfo = cmd.GetWorkspaceFromPath(path);

            if (wkInfo == null)
                return null;

            return cmd.GetWorkspaceTree(wkInfo, path, true);
        }

        internal void Move(string src, string dst)
        {
            Init();

            GetBaseCommands().Move(src, dst,
                MoveModifiers.Silent | MoveModifiers.ThrowExOnPrivateDst);
        }

        internal WorkspaceTreeNode Checkout(string path, string comment)
        {
            Init();

            Console.WriteLine("Going to checkout {0}", path);
            WkTriggerResult result = GetBaseCommands().CheckOut(
                new string[] { path }, false);

            return result.WkNodes[0];
        }*/

        void Init()
        {
            if (bInitialized)
                return;

            try
            {
                if (mClientConfFile != null)
                    ClientConfig.Init(mClientConfFile);
                else
                    ClientConfig.Get();

                Localization.Init(ClientConfig.Get().GetLanguage());
            }
            catch (Exception)
            {
                Localization.Init("");
                throw;
            }

            ClientConfig.Get();
            if (mWkServer != null)
                ClientConfig.Get().SetForcedWorkspaceServer(mWkServer);

            bInitialized = true;
        }

        IBaseCommands GetBaseCommands()
        {
            return new BaseCommandsImpl(true);
        }

        void InitFromMarkerSpec(string treespec)
        {
            SpecGenerator spg = new SpecGenerator();

            // give a try with the marker
            MarkerSpec markerSpec = spg.GenMarkerSpec(true, treespec);

            if (markerSpec == null)
                spg.ExceptIncorrectSpec(treespec);

            SpecResolver spr = new SpecResolver();

            mRepInfo = spr.GetRepInfo(markerSpec.repSpec);

            MarkerInfo mkInfo = spr.GetObjectInfoFromSpec(markerSpec)
                as MarkerInfo;

            if (mkInfo == null)
                throw new Exception(string.Format(
                    "Spec not found {0}", treespec));

            mChangesetId = mkInfo.Changeset;
        }

        bool bInitialized = false;
        string mWkServer = null;
        string mClientConfFile;
        RepositoryInfo mRepInfo;
        long mChangesetId;
    }
}
