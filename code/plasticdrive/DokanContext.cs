using Dokan;

namespace PlasticDrive
{
    internal class DirectoryContext
    {
        internal static void Set(DokanFileInfo info)
        {
            info.Context = Get();
            info.IsDirectory = true;
        }

        internal static int Get(DokanFileInfo info)
        {
            return info.Context;
        }

        static int Get()
        {
            return System.Threading.Interlocked.Increment(ref mCount);
        }

        static int mCount = 1;
    }

    internal class FileContext
    {
        internal static void Set(DokanFileInfo info, int handle)
        {
            info.Context = handle;
        }

        internal static int Get(DokanFileInfo info)
        {
            return info.Context;
        }
    }
}
