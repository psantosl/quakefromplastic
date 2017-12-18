using System;

using Codice.Client.Common;
using Codice.Client.Commands;
using Codice.Client.BaseCommands;

using Codice.CM.Common;

namespace Codice.Client.GlassFS
{
    internal class Hasher
    {
        internal string HashToHex(string hash)
        {
            StringBuilder hexString = new StringBuilder(hash.Length);
            for (int i = 0; i < hash.Length; i++)
            {
                hexString.Append(((byte)(char)hash[i]).ToString("X2"));
            }
            return hexString.ToString();
        }
    }

    internal class FileCache
    {
        private string mBasePath;

        internal string GetFile(RepositoryInfo repInfo, RevisionInfo revInfo, PlasticAPI api)
        {
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
            mBasePath = Path.Combine(Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location), "filecache");

            if (!Directory.Exists(mBasePath))
                Directory.CreateDirectory(mBasePath);

            // otro comentario pero al final
        }
    }
}