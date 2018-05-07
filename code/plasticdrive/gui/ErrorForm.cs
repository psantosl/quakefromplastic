using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;

using Dokan;

using Codice.Client.Common;

namespace PlasticDrive.Gui
{
    class ErrorForm : Form
    {
        public static void ShowError(string errorMsg)
        {
            MessageBox.Show(errorMsg);
        }
   }
}