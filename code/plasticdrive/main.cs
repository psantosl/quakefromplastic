using System;
using System.Text;
using System.IO;
using System.Reflection;

using Dokan;

using log4net.Config;
using PlasticDrive.Writable.Tree;
using PlasticDrive.Writable.Virtual;

namespace PlasticDrive
{
    class MainApp
    {
        static void Main(string[] argArray)
        {
            // usage: plasticdrive --drive drive csetspec | labelspec [--clientconf client.conf] [-g]
            if (argArray.Length < 1)
            {
                PrintUsage();
                return;
            }

            Args args = Args.Parse(argArray);

            if (args.Drive == null)
                args.Drive = GetFreeDriveLetter();

            string driveLetter = args.Drive + @"\\";

            PlasticAPI plasticAPI = null;

            try
            {
                plasticAPI = new PlasticAPI(args.ClientConf);

                plasticAPI.InitTreeSpec(args.Spec);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                PrintUsage();
                return;
            }

            DokanOptions opt = new DokanOptions();

            opt.DebugMode = false;

            opt.NetworkDrive = false;
            opt.MountPoint = driveLetter[0].ToString();
            opt.RemovableDrive = true;
            opt.ThreadCount = 1;
            opt.UseAltStream = true;
            opt.UseKeepAlive = true;

            if (args.Writable)
                opt.VolumeLabel = "writable plastic - " + args.Spec;
            else
                opt.VolumeLabel = "plastic drive - " + args.Spec;

            ConfigureLogging(null);

            PlasticDrive drive = new PlasticDrive();
            drive.Options = opt;
            drive.DriveLetter = driveLetter;

            if (args.Writable)
                drive.PlasticFs = (IPlasticFs)InitWritable.Create(plasticAPI, args.CachePath);
            else
                drive.PlasticFs = new Readonly.ReadonlyFs(driveLetter, args.CachePath, plasticAPI);

            System.Threading.Thread thread = new System.Threading.Thread(LaunchOnThread);
            thread.Start(drive);

            while (thread.IsAlive && !drive.PlasticFs.IsInitialized() && !IsDllError())
            {
                System.Threading.Thread.Sleep(100);
            }

            if (!thread.IsAlive || IsDllError())
            {
                if (args.UseGraphical)
                {
                    mDokanInitErrorMessage.AppendLine();
                    mDokanInitErrorMessage.AppendLine("You can press CTRL+C here to copy this text");
                    Gui.ErrorForm.ShowError(mDokanInitErrorMessage.ToString());
                }
                else
                {
                    Console.Write(mDokanInitErrorMessage.ToString());
                }

                return;
            }

            if (args.UseGraphical)
            {
                Console.WriteLine("open {0}", args.Drive);
                OpenFileWith("explorer.exe", args.Drive, "/root,");
            }

            while (true)
                System.Threading.Thread.Sleep(400);

            Gui.TrayApp.RunApp(
                driveLetter, opt.VolumeLabel);

            if (drive.PlasticFs != null)
                drive.PlasticFs.Stop();
        }

        static class InitWritable
        {
            internal static IPlasticFs Create(PlasticAPI api, string cachePath)
            {
                var treeContent = api.GetTreeContent(
                    Codice.CM.Common.InternalNames.RootDir, true);

                WorkspaceContent workspaceContent =
                    CreateWorkspaceContentFromSelector.Create(DateTime.Now, treeContent);

                Node dotPlastic = Writable.Virtual.DotPlastic.Add(
                    workspaceContent.GetTree(),
                    Codice.Client.Common.ClientConfig.Get().GetWkConfigDir(),
                    DateTime.Now);

                FileHandles handles = new FileHandles();

                Writable.WorkspaceLocalFiles tempStorage = new Writable.WorkspaceLocalFiles(cachePath, handles);

                VirtualFiles virtualFiles = new VirtualFiles();

                Node plasticWkTree = Writable.Virtual.DotPlastic.AddPlasticWkTree(
                    dotPlastic, DateTime.Now);

                var plasticFs = new Writable.PlasticFileSystem(
                    workspaceContent,
                    cachePath,
                    api,
                    handles,
                    tempStorage,
                    virtualFiles);

                virtualFiles.Add(
                    plasticWkTree.GetNodeId(),
                    new PlasticWkTree(
                        plasticWkTree,
                        plasticFs,
                        tempStorage,
                        handles));

                Node plasticWkChangesTree = Writable.Virtual.DotPlastic.AddPlasticWkChangesTree(
                    dotPlastic, DateTime.Now);

                virtualFiles.Add(
                    plasticWkChangesTree.GetNodeId(),
                    new PlasticChanges(
                        plasticWkChangesTree.GetNodeId(),
                        workspaceContent,
                        tempStorage,
                        handles));

                Node plasticWorkspace = Writable.Virtual.DotPlastic.AddPlasticWorkspace(
                    dotPlastic, DateTime.Now);

                virtualFiles.Add(
                    plasticWorkspace.GetNodeId(),
                    new PlasticWorkspace(
                        Guid.NewGuid(),
                        "dynamicWk",
                        plasticWorkspace,
                        tempStorage,
                        handles));

                Node plasticSelector = Writable.Virtual.DotPlastic.AddPlasticSelector(
                    dotPlastic, DateTime.Now);

                virtualFiles.Add(
                    plasticSelector.GetNodeId(),
                    new PlasticSelector(
                        plasticSelector,
                        workspaceContent.GetTree(),
                        api,
                        tempStorage,
                        handles,
                        plasticFs));

                return plasticFs;
            }
        }

        class PlasticDrive
        {
            internal DokanOptions Options;
            internal string DriveLetter;
            internal IPlasticFs PlasticFs;
        }

        static void LaunchOnThread(object o)
        {
            PlasticDrive drive = o as PlasticDrive;

            StringBuilder output = new StringBuilder();

            try
            {
                int status = DokanNet.DokanMain(
                     drive.Options,
                     drive.PlasticFs as DokanOperations);

                switch (status)
                {
                    case DokanNet.DOKAN_DRIVE_LETTER_ERROR:
                        output.AppendLine("Drive letter error");
                        break;
                    case DokanNet.DOKAN_DRIVER_INSTALL_ERROR:
                        output.AppendLine("Driver install error");
                        break;
                    case DokanNet.DOKAN_MOUNT_ERROR:
                        output.AppendLine("Mount error");
                        break;
                    case DokanNet.DOKAN_START_ERROR:
                        output.AppendLine("Start error");
                        break;
                    case DokanNet.DOKAN_ERROR:
                        output.AppendLine("Unknown error");
                        break;
                    case DokanNet.DOKAN_SUCCESS:
                        return;
                    default:
                        output.AppendLine(string.Format("Unknown status: {0}", status));
                        break;
                }
            }
            catch (DllNotFoundException e)
            {
                output.AppendLine(e.Message);
                output.AppendLine("plasticdrive is not able to find a dll, most likely Dokan.dll.");
                output.AppendLine("If that's the case download and install " +
                    " https://www.plasticscm.com/externalcontent/dokan/DokanInstall_0.7.4.exe");
                output.AppendLine("If still it can't find it, maybe you need a" +
                    " reboot or place the dll together with the plasticdrive binaries");
            }

            output.AppendLine("If you're here it means you don't have the Dokan FS Driver installed");
            output.AppendLine("If that's the case download and install " +
                " https://www.plasticscm.com/externalcontent/dokan/DokanInstall_0.7.4.exe");

            lock (mLock)
            {
                mDokanInitErrorMessage = output;
            }
        }

        static bool IsDllError()
        {
            lock (mLock)
            {
                return mDokanInitErrorMessage != null && 
                        !string.IsNullOrEmpty(mDokanInitErrorMessage.ToString());
            }
        }

        static void ConfigureLogging(string file)
        {
            if( file == null )
                file = "plasticdrive.log.conf";
            string log4netpath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), file);
            XmlConfigurator.ConfigureAndWatch(new FileInfo(log4netpath));
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage: plasticdrive [--drive drive] csetspec | labelspec [--clientconf client.conf] [-g] [--cachepath path]");
            Console.WriteLine(@"Example: plasticdrive --drive z: 127@plasticrepo@localhost:8087 c:\users\pablo\plastic\client\client.conf --cachepath F:\plasticdrivecache ");
            Console.WriteLine(@"Example: plasticdrive --drive z: lb:BL170@plasticrepo@localhost:8087 // will create the cache path on the execution directory");
            Console.WriteLine(@"Example: plasticdrive lb:BL170@plasticrepo@localhost:8087 // will choose a drive automatically");
        }

        static void OpenFileWith(string exePath, string path, string arguments)
        {
            if (path == null)
                return;

            using (System.Diagnostics.Process process = new System.Diagnostics.Process())
            {
                process.StartInfo.WorkingDirectory = Path.GetDirectoryName(path);
                if (exePath != null)
                {
                    process.StartInfo.FileName = exePath;
                    //Pre-post insert quotes for fileNames with spaces.
                    process.StartInfo.Arguments = string.Format("{0}\"{1}\"", arguments, path);
                }
                else
                {
                    process.StartInfo.FileName = path;
                    process.StartInfo.WorkingDirectory = Path.GetDirectoryName(path);
                }

                if (!path.Equals(process.StartInfo.WorkingDirectory))
                {
                    process.Start();
                }
            }
        }

        static string GetFreeDriveLetter()
        {
            DriveInfo[] drives = DriveInfo.GetDrives();

            char letter = 'z';

            while (letter > 'c')
            {
                string letterDrive = letter + ":\\";

                bool found = false;

                foreach (DriveInfo driveInfo in drives)
                {
                    if (driveInfo.Name.ToLower() == letterDrive)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    return letter + ":";

                --letter;
            }

            return null;
        }

        class Args
        {
            internal string Drive;
            internal string Spec;
            internal string ClientConf;
            internal string CachePath;
            internal bool UseGraphical;
            internal bool Writable = false;

            internal static Args Parse(string[] args)
            {
                Args result = new Args();

                int argIndex = 0;

                while (argIndex < args.Length)
                {
                    string arg = args[argIndex];

                    switch (arg.ToLower())
                    {
                        case "--drive":
                            result.Drive = SafeGet(args, ++argIndex);
                            break;
                        case "--clientconf":
                            result.ClientConf = SafeGet(args, ++argIndex);
                            break;
                        case "--cachepath":
                            result.CachePath = SafeGet(args, ++argIndex);
                            break;
                        case "-g":
                            result.UseGraphical = true;
                            break;
                        case "-w":
                            result.Writable = true;
                            break;
                        default:
                            result.Spec = args[argIndex];
                            break;
                    }

                    ++argIndex;
                }

                return result;
            }

            static string SafeGet(string[] args, int index)
            {
                if (index < args.Length)
                    return args[index];

                return null;
            }
        }

        static object mLock = new object();
        static StringBuilder mDokanInitErrorMessage;
    }
}
// http://www.codeproject.com/KB/cs/ntfsstreams.aspx?df=100&forumid=4446&exp=0&select=890716
// http://www.delphi-central.com/tutorials/File_Summary.aspx