using System;

using Codice.Client.Common;
using Codice.Client.Commands;
using Codice.Client.BaseCommands;

using Codice.CM.Common;

namespace Codice.Client.GlassFS
{
    internal class FileCache
    {
        private string mBasePath;

        internal string GetFile(RepositoryInfo repInfo, RevisionInfo revInfo, PlasticAPI api)
        {
            // 1st change
            string hash = HashToHex(revInfo.Hash);

            string subdir = Path.Combine(mBasePath, string.Concat(hash[0] , hash[1]));

            if (!Directory.Exists(subdir))
                Directory.CreateDirectory(subdir);

            string file = Path.Combine(subdir, hash);

            if (File.Exists(file))
                return file;

            api.GetFile(repInfo, revInfo, file);

            return file;
        }

        internal FileCache()
        {
            // 2nd change
            mBasePath = Path.Combine(Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location), "filecache");

            if (!Directory.Exists(mBasePath))
                Directory.CreateDirectory(mBasePath);

            // 3rd change
        }
    }
}