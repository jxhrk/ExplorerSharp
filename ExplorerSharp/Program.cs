using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using ExplorerSharp.Thumbnails;
using HarfBuzzSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ExplorerSharp
{

    internal class Program
    {
        static bool DEBUG_MULTIPROCESS = true;

        static bool DEBUG_OPEN_OG_EXPLORER = false;

        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            if (DEBUG_MULTIPROCESS)
            {
                AppBuilder b = BuildAvaloniaApp();
                b.Start(MainDebug, args);
                return;
            }

            if (GetInstancesCount() > 1)
            {
                if (args.Length > 0)
                {
                    Send(args[0], args.Contains("-select"));
                }
                else
                {
                    Send("C:\\", false);
                }

                //if (CanOpenExplorerSharp(args))
                //{
                //    if (DEBUG_OPEN_OG_EXPLORER)
                //    {
                //        OpenOriginalExplorer(args[0]);
                //    }

                //    Send(args[0], args.Contains("-select"));
                //}
                //else
                //{
                //    OpenOriginalExplorer(args[0]);
                //}
                return;
            }

            ReadLoop();
            AppBuilder builder = BuildAvaloniaApp();
            builder.Start(Main, args);
        }

        private static bool CanOpenExplorerSharp(string[] args)
        {
            return args.Length > 0 && Path.Exists(args[0]);
        }

        private static void OpenOriginalExplorer(string arg)
        {
            var p = new Process();
            p.StartInfo = new ProcessStartInfo("explorer.exe")
            {
                Arguments = arg,
                UseShellExecute = true,
                Verb = "open"
            };
            p.Start();
        }

        private static async void ReadLoop()
        {
            const int MMF_MAX_SIZE = 265;

            // creates the memory mapped file
            MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen("mmf1", MMF_MAX_SIZE, MemoryMappedFileAccess.ReadWrite);

            MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor();

            await Task.Run(() =>
            {
                while (accessor.CanRead)
                {
                    byte b = accessor.ReadByte(0);
                    byte selecting = accessor.ReadByte(1);

                    if (b == 1)
                    {
                        int len = accessor.ReadInt32(2);
                        char[] text = new char[len];
                        accessor.ReadArray<char>(6, text, 0, len);
                        string path = string.Join("", text);
                        accessor.Write(0, false);

                        Dispatcher.UIThread.Invoke(() =>
                        {
                            MainWindow.OpenNew(path, selecting == 1);
                        });
                    }

                    Thread.Sleep(10);
                }
            });

        }

        private static void Send(string path, bool selecting)
        {



            MemoryMappedFile mmf = MemoryMappedFile.OpenExisting("mmf1", MemoryMappedFileRights.ReadWrite);
            MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor();

            accessor.Write(0, true);
            accessor.Write(1, selecting);
            char[] pathArr = path.ToCharArray();
            accessor.Write(2, pathArr.Length);
            accessor.WriteArray(6, pathArr, 0, pathArr.Length);

            accessor.Dispose();
            mmf.Dispose();
        }

        private static void Main(Application app, string[] args)
        {
            var cts = new CancellationTokenSource();
            app.Run(cts.Token);
        }

        private static void MainDebug(Application app, string[] args)
        {
            var cts = new CancellationTokenSource();
            MainWindow.OpenNew("C:\\");

            Window.WindowClosedEvent.AddClassHandler(typeof(Window), (sender, _) =>
            {
                cts.Cancel();
            });

            app.Run(cts.Token);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();



        static int GetInstancesCount()
        {
            string path = Assembly.GetEntryAssembly().Location;
            string processName = Path.GetFileNameWithoutExtension(path);
            return Process.GetProcessesByName(processName).Length;
        }

    }
}
