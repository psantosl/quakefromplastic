using System;
using System.Reflection;
using System.IO;
using System.Text;

using log4net;

using Codice.CM.Common;

namespace PlasticDrive
{
    internal class FileCache
    {
        string mBasePath;

        const string CACHE_DIR = "plasticdrive-filecache";

        internal FileCache(string cachePath)
        {
            mBasePath = string.IsNullOrEmpty(cachePath) ?
                GetDefaultCacheBasePath() :
                cachePath;

            if (!Directory.Exists(mBasePath))
                Directory.CreateDirectory(mBasePath);
        }

        internal static string HashToHex(string hash)
        {
            StringBuilder hexString = new StringBuilder(hash.Length);
            for (int i = 0; i < hash.Length; i++)
            {
                hexString.Append(((byte)(char)hash[i]).ToString("X2"));
            }
            return hexString.ToString();
        }

        internal string GetFile(
            RepositorySpec repSpec,
            RevisionInfo revInfo,
            string path,
            PlasticAPI api)
        {
            string hash = HashToHex(revInfo.Hash);

            string subdir = Path.Combine(mBasePath, string.Concat(hash[0] , hash[1]));

            if (!Directory.Exists(subdir))
                Directory.CreateDirectory(subdir);

            string file = Path.Combine(subdir, hash);

            if (File.Exists(file))
                return file;

            int ini = Environment.TickCount;
            api.GetFile(repSpec, revInfo, file);

            mLog.DebugFormat(
                "{0} ms download {1}",
                Environment.TickCount - ini, path);

            return file;
        }

        static string GetDefaultCacheBasePath()
        {
            string basePath = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                CACHE_DIR);

            // for testing purposes - use the binary path
            if (Directory.Exists(basePath))
                return basePath;

            // but for real purposes use user's path
            return Path.Combine(
                Codice.Utils.UserConfigFolder.GetFolder(),
                CACHE_DIR);
        }

        static readonly ILog mLog = LogManager.GetLogger("FileCache");
    }
}
