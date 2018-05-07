using System.Runtime.InteropServices;

namespace PlasticDrive
{
    static class DiskFreeSpace
    {
        internal static ulong Get(string dirPath)
        {
            ulong freeBytesAvailable, totalNumberOfBytes, totalNumberOfFreeBytes;
            if (GetDiskFreeSpaceEx(dirPath,
                    out freeBytesAvailable, out totalNumberOfBytes, out totalNumberOfFreeBytes))
            {
                return freeBytesAvailable;
            }

            return 0;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetDiskFreeSpaceEx(string directoryName,
            out ulong freeBytesAvailable,
            out ulong totalNumberOfBytes,
            out ulong totalNumberOfFreeBytes);
    }
}