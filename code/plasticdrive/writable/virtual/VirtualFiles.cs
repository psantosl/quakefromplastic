using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlasticDrive.Writable.Virtual
{
    class VirtualFiles
    {
        internal void Add(uint nodeId, IVirtualFile virtualFile)
        {
            mVirtualFiles.Add(nodeId, virtualFile);
        }

        internal IVirtualFile Get(uint nodeId)
        {
            IVirtualFile result;

            if (mVirtualFiles.TryGetValue(nodeId, out result))
                return result;

            return null;
        }

        Dictionary<uint, IVirtualFile> mVirtualFiles = new Dictionary<uint, IVirtualFile>();
    }
}
