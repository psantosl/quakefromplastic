using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PlasticDrive.Writable.Virtual
{
    interface IVirtualFile
    {
        int CreateFile(FileAccess access, FileShare share, FileMode mode, FileOptions options);
        int CloseFile(int fileHandle);
    }
}
