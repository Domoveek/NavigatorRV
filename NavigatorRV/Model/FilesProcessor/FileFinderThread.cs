using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows;
using System.Threading;
using System.IO;

namespace NavigatorRV.Model
{
    public class FileFinderThread : FileFinder
    {
        //Событие синхронизации - смена директории
        public IntPtr SyncEvent { get; set; }
        // Список файлов - связан с отображаемыми данными
        public ObservableCollection<FileInformation> FilesList;
        // Делегат обновления данных
        protected MainWindow.UpateFilesList _acceptFileList;
        //Таймер
        public Timer CurrentTimer { get; set; }

        public Thread FilesThread { get; set; }

        public FileFinderThread(IntPtr SyncEv, ObservableCollection<FileInformation> FilesCollection, String Path, MainWindow.UpateFilesList AcceptCallBack
            , Timer IconTimer, Thread CurrentThread) : base(Path)
        {
            SyncEvent = SyncEv;
            FilesList = FilesCollection;
            _acceptFileList = AcceptCallBack;
            CurrentTimer = IconTimer;

            FilesThread = CurrentThread;
        }

        //Thread function
        public void ChangeNotificationThread()
        {
            IntPtr[] Events = null;
            try
            {
                ObservableCollection<FileInformation> Source = new ObservableCollection<FileInformation>();
                String Path = MonitoringPath;
                Events = new IntPtr[2];
                Events[0] = SyncEvent;
                Events[1] = WinAPI.FindFirstChangeNotification(Path, false, WinAPI.FILE_NOTIFY_CHANGE_FILE_NAME | WinAPI.FILE_NOTIFY_CHANGE_DIR_NAME | WinAPI.FILE_NOTIFY_CHANGE_SIZE | WinAPI.FILE_NOTIFY_LAST_WRITE);
                if (Events[1] == WinAPI.INVALID_HANDLE_VALUE)
                {
                    if (Path.Length > 3)
                        Source.Add(new FileInformation("..", "Folder", "Folder", "", ""));

                    Application.Current.Dispatcher.BeginInvoke(_acceptFileList, new object[] { Source, FilesList, CurrentTimer });
                    return;
                }
                UInt32 State = WinAPI.WAIT_OBJECT_0 + 1;

                while (State == WinAPI.WAIT_OBJECT_0 + 1)
                {

                    FileSystemMutex.Instance.Enter((Action)(() =>
                    {
                        Source = new ObservableCollection<FileInformation>();
                        foreach (var x in FilesCollection(Path))
                        {
                            if (x.FileName == "..")
                                x.LastChangeDate = "";
                            if (x.FileName != ".")//Canceled one of directories
                                Source.Add(x);
                        }

                        Application.Current.Dispatcher.BeginInvoke(_acceptFileList, new object[] { Source, FilesList, CurrentTimer });
                    }));

                    State = WinAPI.WaitForMultipleObjects(2, Events, false, WinAPI.INFINITE);
                    WinAPI.FindNextChangeNotification(Events[1]);

                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (Events != null)
                    WinAPI.FindCloseChangeNotification(Events[1]);
            }
        }

        //Функция объявляющая поиск файлов для работы со стороннего потока
        public static void FileFinderThreadInit(object ThreadParams)
        {
            if (ThreadParams is FileFinderThread)
            {
                (ThreadParams as FileFinderThread).ChangeNotificationThread();
            }
        }
    }
}
