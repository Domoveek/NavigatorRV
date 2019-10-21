using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Collections.ObjectModel;

namespace NavigatorRV.Model
{
    public enum Mode
    {
        Copy = 1,
        Cut = 2
    }

    public class CopyInform
    {
        protected static Semaphore CopySemaphore = new Semaphore(5, 5);
        protected static Semaphore AllCopyTasks = new Semaphore(10, 10);

        public String FromDir { get; set; }
        public String ToDir { get; set; }
        public String CurrentSize { get; set; }

        public Mode CMode;

        private ObservableCollection<CopyInform> DelegateInf;

        public Boolean Complete = false;

        public String CurFileName;
        private CopyInform() { }
        Thread _copyThr;
        private MainWindow.CheckCopyFunctionList DeleteItem;
        /*//Change Path delegate
        protected ChangePath Changer = ChangePathName;
        protected delegate void ChangePath(ref String Dest,ref String NewPath);
        protected static void ChangePathName(ref String Dest, ref String NewPath)
        {
            Dest = NewPath;
        }*/

        //Change Size delegate
        protected ChangeSize ChangerSize = ChangeCurrentSize;
        protected delegate void ChangeSize(ref UInt64 Dest, Int32 Piece);
        protected static void ChangeCurrentSize(ref UInt64 Dest, Int32 Piece)
        {
            Dest += (UInt64)Piece;
        }

        public void WaitThread()
        {
            _copyThr.Join();
        }

        public CopyInform(String From, String To, String FileName, String Size, ObservableCollection<CopyInform> delegateInformation, Mode CurMode, MainWindow.CheckCopyFunctionList CheckList)
        {
            FromDir = From + FileName;
            ToDir = To + FileName;
            CurrentSize = Size;
            CurFileName = FileName;
            DelegateInf = delegateInformation;
            CMode = CurMode;

            DeleteItem = CheckList;

            AllCopyTasks.WaitOne();

            _copyThr = new Thread(new ThreadStart(CopyMethod));
            _copyThr.Start();



        }


        protected String GetAnotherDestinationPath(String currentPath)
        {
            StringBuilder NewStr = new StringBuilder(currentPath);

            while (NewStr[NewStr.Length - 1] != '\\' && NewStr[NewStr.Length - 1] != '/')
                NewStr.Remove(NewStr.Length - 1, 1);

            CurFileName = "Copy_" + CurFileName;
            NewStr.Append(CurFileName);



            return NewStr.ToString();
        }

        protected void CopyFile(String From, String To)
        {
            CopySemaphore.WaitOne();
            try
            {
                IntPtr ReadHandle = WinAPI.CreateFile(From, WinAPI.GENERIC_READ, 0, IntPtr.Zero, WinAPI.OPEN_EXISTING, 0, IntPtr.Zero);

                if (ReadHandle == WinAPI.INVALID_HANDLE_VALUE)
                {
                    //can't open Source file
                    throw new Exception("Can't open source file");

                }
                IntPtr WriteHandle = WinAPI.INVALID_HANDLE_VALUE;
                while (WriteHandle == WinAPI.INVALID_HANDLE_VALUE && CurFileName.Length < 256)
                {
                    WriteHandle = WinAPI.CreateFile(To, WinAPI.GENERIC_WRITE, 0, IntPtr.Zero, WinAPI.CREATE_NEW, 0, IntPtr.Zero);

                    if (WriteHandle == WinAPI.INVALID_HANDLE_VALUE)
                    {
                        To = GetAnotherDestinationPath(To);
                    }
                }
                if (WriteHandle == WinAPI.INVALID_HANDLE_VALUE)
                {
                    WinAPI.CloseHandle(ReadHandle);
                    throw new Exception("Can't open output file");
                }

                const int Size = 1024 * 1024 * 16;

                Byte[] Buffer = new Byte[Size];

                Int32[] Readed = new Int32[1];
                Boolean ReadResult;
                do
                {
                    ReadResult = WinAPI.ReadFile(ReadHandle, Buffer, Size, Readed, 0);
                    Int32 ToWrite = Readed[0];
                    ReadResult = ReadResult & WinAPI.WriteFile(WriteHandle, Buffer, ToWrite, Readed, 0);

                } while (ReadResult && Readed[0] == Size);

                WinAPI.CloseHandle(ReadHandle);
                WinAPI.CloseHandle(WriteHandle);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                CopySemaphore.Release();
            }

        }

        protected void CopyMethod()
        {
            try
            {
                if (CurrentSize == "Folder" && FromDir != ".." && FromDir != ".")
                {
                    WinAPI.CreateDirectory(ToDir, IntPtr.Zero);
                    DirectoryCopy(FromDir + '\\', ToDir + '\\');
                }
                else
                    if (CurrentSize != "Folder")
                {
                    try
                    {
                        CopyFile(FromDir, ToDir);
                        if (CMode == Mode.Cut)
                        {
                            WinAPI.DeleteFile(FromDir);
                        }
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Can't copy file", "Warning", MessageBoxButton.OK, MessageBoxImage.Stop);
                    }
                }
                Complete = true;
                if (DeleteItem != null)
                    Application.Current.Dispatcher.BeginInvoke(DeleteItem, DelegateInf);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                AllCopyTasks.Release();
            }

        }

        protected void DirectoryCopy(String From, String To)
        {
            try
            {
                var Files = new FileFinder(From);
                foreach (var x in Files.FilesCollection(From))
                {
                    if (x.FileSize != "Folder")
                    {
                        //Multi-treading copy   
                        new CopyInform(From, To, x.FileName, x.FileSize, null, Mode.Copy, null);
                        //CopyFile(From + x.FileName, To + x.FileName);
                    }
                    if (x.FileSize == "Folder")
                    {
                        if (x.FileName != ".." && x.FileName != ".")
                        {
                            WinAPI.CreateDirectory(To + x.FileName, IntPtr.Zero);
                            DirectoryCopy(From + x.FileName + '\\', To + x.FileName + '\\');
                        }
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Can't copy directory");

            }
        }


    }
}
