using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace CodeWatcher
{
    public static class NativeMethods
    {
        [StructLayout(LayoutKind.Explicit)]
        public struct BY_HANDLE_FILE_INFORMATION
        {
            [FieldOffset(0)]
            public uint FileAttributes;

            [FieldOffset(4)]
            public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;

            [FieldOffset(12)]
            public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;

            [FieldOffset(20)]
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;

            [FieldOffset(28)]
            public uint VolumeSerialNumber;

            [FieldOffset(32)]
            public uint FileSizeHigh;

            [FieldOffset(36)]
            public uint FileSizeLow;

            [FieldOffset(40)]
            public uint NumberOfLinks;

            [FieldOffset(44)]
            public uint FileIndexHigh;

            [FieldOffset(48)]
            public uint FileIndexLow;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetFileInformationByHandle(SafeFileHandle hFile, out BY_HANDLE_FILE_INFORMATION lpFileInformation);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern SafeFileHandle CreateFile([MarshalAs(UnmanagedType.LPTStr)] string filename,
          [MarshalAs(UnmanagedType.U4)] FileAccess access,
          [MarshalAs(UnmanagedType.U4)] FileShare share,
          IntPtr securityAttributes,
          [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
          [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
          IntPtr templateFile);
    }
    class PathUtility
    {
        public static bool IsSameFile(string path1, string path2)
        {
            using (SafeFileHandle sfh1 = NativeMethods.CreateFile(path1, FileAccess.Read, FileShare.ReadWrite,
                IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero))
            {
                if (sfh1.IsInvalid)
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

                using (SafeFileHandle sfh2 = NativeMethods.CreateFile(path2, FileAccess.Read, FileShare.ReadWrite,
                  IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero))
                {
                    if (sfh2.IsInvalid)
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

                    NativeMethods.BY_HANDLE_FILE_INFORMATION fileInfo1;
                    bool result1 = NativeMethods.GetFileInformationByHandle(sfh1, out fileInfo1);
                    if (!result1)
                        throw new IOException(string.Format("GetFileInformationByHandle has failed on {0}", path1));

                    NativeMethods.BY_HANDLE_FILE_INFORMATION fileInfo2;
                    bool result2 = NativeMethods.GetFileInformationByHandle(sfh2, out fileInfo2);
                    if (!result2)
                        throw new IOException(string.Format("GetFileInformationByHandle has failed on {0}", path2));

                    return fileInfo1.VolumeSerialNumber == fileInfo2.VolumeSerialNumber
                      && fileInfo1.FileIndexHigh == fileInfo2.FileIndexHigh
                      && fileInfo1.FileIndexLow == fileInfo2.FileIndexLow;
                }
            }
        }
    }
}
