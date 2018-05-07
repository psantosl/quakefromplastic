using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;

using Dokan;

using Codice.Client.Common;
using Codice.Utils;

namespace PlasticDrive.Gui
{
    class TrayApp: Form
    {
        public static void RunApp(string drive, string info)
        {
            string themePath = Path.GetDirectoryName(ApplicationLocation.GetAppPath());

            string iconPath = themePath + @"\theme\windows\default\icons\icon-16-plasticdrive.ico";

            if (!File.Exists(iconPath))
            {
                Console.WriteLine("Can't run in tray mode because the icon has not been found in the theme: {0}", iconPath);
                return;
            }

            Console.In.Close();
            Console.Out.Flush();
            FreeConsole();
            Application.Run(new TrayApp(drive, info, iconPath));
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool FreeConsole();

        internal TrayApp(string drive, string info, string iconPath)
        {
            InitializeComponent();
            // Create a simple tray menu with only one item.
            mTrayMenu = new ContextMenu();
            mTrayMenu.MenuItems.Add("Unmount " + drive[0] + ":", OnUnmount);
            //trayMenu.MenuItems.Add("Exit", OnExit);

            mMountDrive = drive;

            // Create a tray icon. In this example we use a
            // standard system icon for simplicity, but you
            // can of course use your own custom icon too.
            mTrayIcon = new NotifyIcon();
            mTrayIcon.Text = info;

            mTrayIcon.Icon = new Icon(iconPath);

            // Add menu to tray icon and show it.
            mTrayIcon.ContextMenu = mTrayMenu;
            mTrayIcon.Visible = true;
        }

        protected override void OnLoad(EventArgs e)
        {
            Visible = false; // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.

            base.OnLoad(e);
        }

        void OnUnmount(object sender, EventArgs e)
        {
            DokanNet.DokanRemoveMountPoint(mMountDrive[0].ToString());
            DokanNet.DokanUnmount(mMountDrive[0]);
            Application.Exit();
        }

        void OnExit(object sender, EventArgs e)
        {
            DokanNet.DokanUnmount(mMountDrive[0]);
            Application.Exit();
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                // Release the icon resource.
                if (mTrayIcon != null)
                    mTrayIcon.Dispose();
            }

            base.Dispose(isDisposing);
        }

        void InitializeComponent()
        {
            this.Name = "PlasticDrive";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        string mMountDrive;

        NotifyIcon mTrayIcon;
        ContextMenu mTrayMenu;
    }
}
