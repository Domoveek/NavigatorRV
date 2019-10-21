using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;
using System.Threading;
using System.Windows.Input;

namespace NavigatorRV.Model
{
    static class WinAPI
    {
        #region constants

        //Constants
        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        internal const int MAX_PATH = 260;

        //FileFindFirstChangeNotification Constants
        public static readonly UInt32 FILE_NOTIFY_CHANGE_FILE_NAME = 0x1;
        public static readonly UInt32 FILE_NOTIFY_CHANGE_DIR_NAME = 0x2;
        //private static readonly UInt32 FILE_NOTIFY_CHANGE_ATTRIBUTES = 0x4;
        public static readonly UInt32 FILE_NOTIFY_CHANGE_SIZE = 0x8;
        public static readonly UInt32 FILE_NOTIFY_LAST_WRITE = 0x10;
        //private static readonly UInt32 FILE_NOTIFY_CHANGE_SECURITY = 0x100;

        //Wait_for constant
        public static readonly UInt32 WAIT_OBJECT_0 = 0;
        //TimeConstants
        public static readonly UInt32 INFINITE = 0xFFFFFFFF;
        //Generic access constants and File share constants
        public static readonly UInt32 GENERIC_READ = 0x80000000;
        public static readonly UInt32 GENERIC_WRITE = 0x40000000;
        public static readonly UInt32 FILE_SHARE_DELETE = 0x4;
        public static readonly UInt32 FILE_SHARE_READ = 0x1;
        public static readonly UInt32 FILE_SHARE_WRITE = 0x2;
        //Creation disposition
        public static readonly UInt32 CREATE_ALWAYS = 2;
        public static readonly UInt32 CREATE_NEW = 1;
        public static readonly UInt32 OPEN_ALWAYS = 4;
        public static readonly UInt32 OPEN_EXISTING = 3;
        public static readonly UInt32 TRUNCATE_EXISTING = 5;


        //Structures
        /// <summary>
        /// Find Structure
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        [BestFitMapping(false)]
        public struct WIN32_FIND_DATA
        {
            public FileAttributes FileAttr;
            public FILETIME CreationTime;
            public FILETIME LastAccessTime;
            public FILETIME LastWriteTime;
            public UInt32 FileSizeHight;
            public UInt32 FileSizeLow;
            public int Reserved0;
            public int Reserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            public string FileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string Alternate;
        }
        //Process information structure/
        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        [BestFitMapping(false)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr Process;
            public IntPtr Thread;
            public Int32 ProcessId;
            public Int32 ThreadId;
        }
        #endregion



        #region Winapi functions descripton
        //Functions description
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr FindFirstFile(string FileName, out WIN32_FIND_DATA FindFileData);

        [DllImport("Kernel32.dll", CharSet = CharSet.Ansi)]
        public static extern void GetLogicalDriveStrings([In] UInt32 BufferLength, [Out] Char[] Buffer);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool FindNextFile(IntPtr FindHandle, out WIN32_FIND_DATA FindFileData);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool FindClose(IntPtr FindHandle);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr FindFirstChangeNotification(String PathName, Boolean WatchSubTree, UInt32 NotifyFilter);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern UInt32 WaitForMultipleObjects(UInt32 Count, IntPtr[] Handles, Boolean WaitAll, UInt32 Milliseconds);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern Boolean FindCloseChangeNotification(IntPtr ChangeHandle);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern Boolean FindNextChangeNotification(IntPtr ChangeHandle);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CreateEvent(IntPtr EventAttributes, Boolean ManualReset, Boolean InitialState, String Name);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern Boolean SetEvent(IntPtr Handle);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern Boolean ResetEvent(IntPtr Handle);


        //Files
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CreateFile(String FileName, UInt32 DesiredAccess, UInt32 ShareMode,
                IntPtr SecurityAttr, UInt32 CreationDisposition, UInt32 FlagsAndAttr, IntPtr TemplateFile);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern Boolean CloseHandle(IntPtr Handle);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern Boolean ReadFile(IntPtr File, Byte[] Buffer, int NumberOfBytesToRead, int[] NumberOfBytesRead, int Overlapped);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern Boolean WriteFile(IntPtr File, Byte[] Buffer, int NumberOfBytesToWrite, int[] NumberOfBytesWritten, int Overlapped);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern Boolean CreateDirectory(String PathName, IntPtr SecurityAttr);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern Boolean DeleteFile(String PathName);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern Boolean RemoveDirectory(String Path);
        /*[DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern Boolean CreateProcess(String ApplicationName, String CommandLine,
            IntPtr ProcessAttributes, IntPtr ThreadAttributes, Boolean InheritHandles, UInt32 CreationFlags, 
            IntPtr Environment, IntPtr CurrentDirectory, Byte[] StartupInformation, PROCESS_INFORMATION pi);*/

        #endregion

        #region KeyBoard Scan Code to Unicode
        private enum MapType : uint
        {
            MAPVK_VK_TO_VSC = 0x0,
            MAPVK_VSC_TO_VK = 0x1,
            MAPVK_VK_TO_CHAR = 0x2,
            MAPVK_VSC_TO_VK_EX = 0x3,
        }

        [DllImport("user32.dll")]
        private static extern int ToUnicode(
            uint wVirtKey,
            uint wScanCode,
            byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)]
            StringBuilder pwszBuff,
            int cchBuff,
            uint wFlags);

        [DllImport("user32.dll")]
        private static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, MapType uMapType);

        public static char GetCharFromKey(Key key)
        {
            char ch = ' ';

            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            byte[] keyboardState = new byte[256];
            GetKeyboardState(keyboardState);

            uint scanCode = MapVirtualKey((uint)virtualKey, MapType.MAPVK_VK_TO_VSC);
            StringBuilder stringBuilder = new StringBuilder(2);

            int result = ToUnicode((uint)virtualKey, scanCode, keyboardState, stringBuilder, stringBuilder.Capacity, 0);
            switch (result)
            {
                case -1:
                    break;
                case 0:
                    break;
                case 1:
                    {
                        ch = stringBuilder[0];
                        break;
                    }
                default:
                    {
                        ch = stringBuilder[0];
                        break;
                    }
            }
            return ch;
        }
        #endregion
    }
}
