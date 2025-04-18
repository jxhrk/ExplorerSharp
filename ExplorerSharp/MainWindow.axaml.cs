using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using ExplorerSharp.Controls;
using ExplorerSharp.Thumbnails;
using Microsoft.WindowsAPICodePack.Shell;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ExplorerSharp
{
    public class FileInfo : INotifyPropertyChanged
    {
        public string Path;

        public string Name { get; set; }


        public bool IsDirectory;
        FileAttributes Attributes;
        public DateTime LastModified { get; set; }
        public long Size { get; set; }

        //public Bitmap Thumbnail { get; set; }


        private Bitmap thumbnail;


        public Bitmap Thumbnail
        {
            set
            {
                thumbnail = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Thumbnail)));
            }
            get { return thumbnail; }
        }

        public event PropertyChangedEventHandler? PropertyChanged;




        public FileInfo(string path, string name, bool isDirectory)
        {
            Path = path;
            Name = name;
            IsDirectory = isDirectory;

            System.IO.FileInfo info = new System.IO.FileInfo(path);
            Attributes = info.Attributes;
            LastModified = info.LastWriteTime;

            
            Size = isDirectory ? 0 : info.Length;

            //if (!IsDirectory)
            //{
                Task.Run(() =>
                {
                    Thumbnail = GetThumbnail(path);
                });
            //}
        }

        public override string ToString()
        {
            return Name;
        }

        public bool IsHidden()
        {
            return Attributes.HasFlag(FileAttributes.Hidden);
        }

        public bool IsSystem()
        {
            return Attributes.HasFlag(FileAttributes.System);
        }


        private Bitmap GetThumbnail(string path)
        {
            //ShellFile shellFile = ShellFile.FromFilePath(path);
            //Bitmap shellThumb = ConvertToAvaloniaBitmap(shellFile.Thumbnail.ExtraLargeBitmap);
            //return shellThumb;
            var a = WindowsThumbnailProvider.GetThumbnail(path,16,16, ThumbnailOptions.IconOnly);
            var b = ConvertToAvaloniaBitmap(a);

            return b;
        }

        public static Bitmap ConvertToAvaloniaBitmap(System.Drawing.Bitmap bitmap)
        {
            if (bitmap == null) return null;

            Bitmap AvIrBitmap;
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                AvIrBitmap = new Bitmap(memory);
            }
            return AvIrBitmap;

        }

    }

    public partial class MainWindow : Window
    {
        string CurrentPath;

        public MainWindow(string path, bool selecting = false)
        {
            InitializeComponent();

            DirectoryContent.Main = this;
            NavPanel.Main = this;

            if (selecting)
            {
                GetDirAndFile(path, out string dir, out string file);
                NavigateToDir(dir, file);
            }
            else
            {
                NavigateToDir(path);
            }

        }
        public MainWindow()
        {
            InitializeComponent();
        }

        public static void OpenNew(string path, bool selecting = false)
        {
            Trace.WriteLine(path);
            new MainWindow(path, selecting).Show();
        }

        public void NavigateToDir(string dir, string file = "")
        {
            if (dir.EndsWith("\\") && !dir.EndsWith(":\\"))
            {
                dir = dir[..^1];
            }
            else if (dir.EndsWith(":"))
            {
                dir += "\\";
            }

            string dirDisplayName = Utils.GetDirDisplayName(dir);
            NavPanel.ChangePath(dir, dirDisplayName);
            Title = dirDisplayName;
            CurrentPath = dir;

            DriveInfo.GetDrives();

            List<FileInfo> files = new List<FileInfo>();
            foreach (string path in Directory.GetDirectories(dir))
            {
                files.Add(new FileInfo(path, Path.GetFileName(path), true));
            }

            foreach (string path in Directory.GetFiles(dir))
            {
                files.Add(new FileInfo(path, Path.GetFileName(path), false));
            }

            files = FilterContent(files, true, true);

            ((IDirectoryContentPresenter)DirectoryContent).SetContent(files);

            if (file != "")
            {
                int index = files.FindIndex(o => o.Name == file);
                if (index != -1)
                {
                    ((IDirectoryContentPresenter)DirectoryContent).SelectFile(index);
                }
            }
        }

        private void GetDirAndFile(string path, out string dir, out string file)
        {
            dir = Path.GetDirectoryName(path);
            file = Path.GetFileName(path);
        }

        private List<FileInfo> FilterContent(List<FileInfo> unfiltered, bool filterHidden, bool filterSystem)
        {
            List<FileInfo> files = [.. unfiltered];
            foreach (FileInfo file in unfiltered)
            {
                if ((filterHidden && file.IsHidden()) ||
                    (filterSystem && file.IsSystem()))
                {
                    files.Remove(file);
                }
            }
            return files;
        }

        public void DoubleClicked(FileInfo info)
        {
            if (info == null) return;


            if (info.IsDirectory)
            {
                NavigateToDir(info.Path);
            }
            else
            {
                var p = new Process();
                p.StartInfo = new ProcessStartInfo(info.Path)
                {
                    UseShellExecute = true
                };
                try
                {
                    p.Start();
                }
                catch (Win32Exception e)
                {
                    //https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes--1000-1299-

                    switch (e.NativeErrorCode)
                    {
                        case 1155: //ERROR_NO_ASSOCIATION
                            AssociationNotFoundWindow selector = new AssociationNotFoundWindow();
                            selector.Show();
                            return;
                        case 1223: //ERROR_CANCELLED
                            return;
                    }

                    throw e;
                }
            }

        }

        public void MouseReleased(FileInfo info, PointerReleasedEventArgs args)
        {
            if (info == null || !info.IsDirectory) return;

            if (args.InitialPressMouseButton == MouseButton.Middle)
            {
                OpenNew(info.Path);
            }
        }

        public void HistoryBack()
        {
            // TODO: history
            DirectoryInfo info = Directory.GetParent(CurrentPath);
            if (info == null) return;
            NavigateToDir(info.FullName);

        }


    }
}