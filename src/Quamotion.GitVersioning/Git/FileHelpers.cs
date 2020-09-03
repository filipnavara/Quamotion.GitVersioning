using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Quamotion.GitVersioning.Git
{
    public static class FileHelpers
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern SafeFileHandle CreateFile(
        [MarshalAs(UnmanagedType.LPTStr)] string filename,
        [MarshalAs(UnmanagedType.U4)] FileAccess access,
        [MarshalAs(UnmanagedType.U4)] FileShare share,
        IntPtr securityAttributes,
        [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
        [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
        IntPtr templateFile);

        private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static bool TryOpen(string path, out Stream stream)
        {
            if (IsWindows)
            {
                var handle = CreateFile(path, FileAccess.Read, FileShare.Read, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);

                if (!handle.IsInvalid)
                {
                    stream = new FileStream(handle, FileAccess.Read);
                    return true;
                }
                else
                {
                    stream = null;
                    return false;
                }
            }
            else
            {
                if (!File.Exists(path))
                {
                    stream = null;
                    return false;
                }

                stream = File.OpenRead(path);
                return true;
            }
        }
    }
}
