using System;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Win32;

namespace ExplorerSharp.Thumbnails
{
    [Flags]
    public enum ThumbnailOptions
    {
        RESIZETOFIT = 0x00,
        BiggerSizeOk = 0x01,
        InMemoryOnly = 0x02,
        IconOnly = 0x04,
        ThumbnailOnly = 0x08,
        InCacheOnly = 0x10,
    }

    public static class WindowsThumbnailProvider
    {

        // Based on https://stackoverflow.com/questions/21751747/extract-thumbnail-for-any-file-in-windows
        private const string IShellItem2Guid = "7E9FB0D3-919F-4307-AB2E-9B1860310C93";

        internal enum HResult
        {
            Ok = 0x0000,
            False = 0x0001,
            InvalidArguments = unchecked((int)0x80070057),
            OutOfMemory = unchecked((int)0x8007000E),
            NoInterface = unchecked((int)0x80004002),
            Fail = unchecked((int)0x80004005),
            ExtractionFailed = unchecked((int)0x8004B200),
            ElementNotFound = unchecked((int)0x80070490),
            TypeElementNotFound = unchecked((int)0x8002802B),
            NoObject = unchecked((int)0x800401E5),
            Win32ErrorCanceled = 1223,
            Canceled = unchecked((int)0x800704C7),
            ResourceInUse = unchecked((int)0x800700AA),
            AccessDenied = unchecked((int)0x80030005),
        }

        [ComImport]
        [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IShellItemImageFactory
        {
            [PreserveSig]
            HResult GetImage(
            [In, MarshalAs(UnmanagedType.Struct)] NativeSize size,
            [In] ThumbnailOptions flags,
            [Out] out nint phbm);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NativeSize
        {
            private int width;
            private int height;

            public int Width
            {
                set { width = value; }
            }

            public int Height
            {
                set { height = value; }
            }
        }

        public static System.Drawing.Bitmap GetThumbnail(string fileName, int width, int height, ThumbnailOptions options)
        {
            //return ExtractIcon(fileName);

            nint hBitmap = GetHBitmap(fileName, width, height, options);
            if (hBitmap == 0) return null;
            System.Drawing.Bitmap bitmap = GetBitmapFromHBitmap(hBitmap);
            NativeMethods.DeleteObject(hBitmap);
            return bitmap;
        }



        private static System.Drawing.Bitmap GetBitmapFromHBitmap(IntPtr nativeHBitmap)
        {
            System.Drawing.Bitmap bmp = System.Drawing.Bitmap.FromHbitmap(nativeHBitmap);

            if (System.Drawing.Bitmap.GetPixelFormatSize(bmp.PixelFormat) < 32)
                return bmp;

            BitmapData bmpData;

            System.Drawing.Rectangle bmBounds = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
            bmpData = bmp.LockBits(bmBounds, ImageLockMode.ReadOnly, bmp.PixelFormat);
            bmp.UnlockBits(bmpData);

            return new System.Drawing.Bitmap(
                    bmpData.Width,
                    bmpData.Height,
                    bmpData.Stride,
                    PixelFormat.Format32bppArgb,
                    bmpData.Scan0);
        }



        private static nint GetHBitmap(string fileName, int width, int height, ThumbnailOptions options)
        {
            nint hBitmap = nint.Zero;
            IShellItem nativeShellItem = null;

            try
            {
                Guid shellItem2Guid = new Guid(IShellItem2Guid);
                int retCode = NativeMethods.SHCreateItemFromParsingName(fileName, nint.Zero, ref shellItem2Guid, out nativeShellItem);

                if (retCode != 0)
                    throw Marshal.GetExceptionForHR(retCode);

                NativeSize nativeSize = new NativeSize
                {
                    Width = width,
                    Height = height,
                };

                HResult hr = ((IShellItemImageFactory)nativeShellItem).GetImage(nativeSize, options, out hBitmap);

                // if extracting image thumbnail and failed, extract shell icon
                if (options == ThumbnailOptions.ThumbnailOnly && hr == HResult.ExtractionFailed)
                {
                    hr = ((IShellItemImageFactory)nativeShellItem).GetImage(nativeSize, ThumbnailOptions.IconOnly, out hBitmap);
                }

                if (hr != HResult.Ok)
                {
                    throw Marshal.GetExceptionForHR((int)hr);
                }

                return hBitmap;
            }
            catch (Exception e)
            {
                return 0;
            }
            finally
            {
                if (nativeShellItem != null)
                {
                    Marshal.ReleaseComObject(nativeShellItem);
                }
            }
        }

        public static System.Drawing.Bitmap ExtractIcon(string fileName)
        {
            // Extracts the icon associated with the file
            using (System.Drawing.Icon thumbnailIcon = System.Drawing.Icon.ExtractAssociatedIcon(fileName))
            {
                // Convert to Bitmap
                System.Drawing.Bitmap bitmap = thumbnailIcon.ToBitmap();
                //{
                    return bitmap;
                //}
            }
        }
    }
}
