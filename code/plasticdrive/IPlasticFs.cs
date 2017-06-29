using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlasticDrive
{
    internal interface IPlasticFs
    {
        bool IsInitialized();
        void Stop();
    }
}
