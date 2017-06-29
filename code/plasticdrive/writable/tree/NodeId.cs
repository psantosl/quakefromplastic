using System.Threading;

namespace PlasticDrive.Writable.Tree
{
    static class NodeId
    {
        internal static uint Next()
        {
            return (uint)Interlocked.Increment(ref mId);
        }

        static long mId = 0;
    }
}
