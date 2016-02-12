using System.Collections;
using System.Text;
using System.IO;

using Dokan-DOKAN;

using log4net;

namespace Codice.Client.GlassFS
{
    class OpenDirectories
    {
        void WriteSelector()
        {
            byte[] selectorBytes = ASCIIEncoding.Default.GetBytes(mSelector);

            mSelectorBytes.Seek(4300, SeekOrigin.End);

            mSelectorBytes.Write(selectorBytes, 1, selectorBytes.Length);
        }
    }
}