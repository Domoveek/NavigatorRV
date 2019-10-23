using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using NavigatorRV.Model;
using System.IO;
using System.Diagnostics;
using System.Windows.Interop;
using System.IO.Compression;

namespace NavigatorRV
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //dynamic collections to store files
        Drivers _DriversLeft = new Drivers();
        Drivers _DriversRight = new Drivers();
        /*ObservableCollection<FileInformation> _FileDescriptionLeft = new ObservableCollection<FileInformation>();
        ObservableCollection<FileInformation> _FileDescriptionRight = new ObservableCollection<FileInformation>();*/

        //Threads
        IntPtr _eventLeft = WinAPI.CreateEvent(IntPtr.Zero, true, false, null);
        IntPtr _eventRight = WinAPI.CreateEvent(IntPtr.Zero, true, false, null);

        FileFinderThread _filesLeft = null;
        FileFinderThread _filesRight = null;
        protected Boolean _LastFocusedLeft = true;

        //Properties of dynamic collections to store files
        public ObservableCollection<FileInformation> FileDescriptionLeft { get; protected set; }
        public ObservableCollection<FileInformation> FileDescriptionRight { get; protected set; }
        public ObservableCollection<CopyInform> FilesCopyQueue { get; protected set; }

        public Drivers DriversEnumLeft { get { return _DriversLeft; } }
        public Drivers DriversEnumRight { get { return _DriversRight; } }

        protected Settings _userSettings;

        public Boolean DarkTheme { get; set; }

        //First init
        public MainWindow()
        {

            DarkTheme = true;

            FileDescriptionLeft = new ObservableCollection<FileInformation>();
            FileDescriptionRight = new ObservableCollection<FileInformation>();
            FilesCopyQueue = new ObservableCollection<CopyInform>();
            StringBuilder DefaultPath = new StringBuilder(Environment.CommandLine);


            //Готовим настройки
            _userSettings = new Settings();
            if (_userSettings.isIconShowing)
            {
                _iconUpdaterLeft = new Timer(UpdateLeft, null, Timeout.Infinite, Timeout.Infinite);
                _iconUpdaterRight = new Timer(UpdateRight, null, Timeout.Infinite, Timeout.Infinite);
            }

            InitializeComponent();
            //Если запускали с параметром командной строки
            if (DefaultPath != null && DefaultPath[0] != DefaultPath[DefaultPath.Length - 2])
            {

                DefaultPath.Remove(0, 1);
                while (DefaultPath[0] != '\"')
                    DefaultPath.Remove(0, 1);
                DefaultPath.Remove(0, 2);
                PathLeft.Text = DefaultPath.ToString();
            }
            //Если параметр - развернутое окно
            if (_userSettings.AllDock)
            {
                WindowState = System.Windows.WindowState.Maximized;
            }


            _dragResetTimer = new Timer(ResetTimer, null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Иконки файлов
        /// </summary>
        #region TimerUpdaters
        //Таймеры на каждую таблицу
        Timer _iconUpdaterLeft = null, _iconUpdaterRight = null;
        Mutex UpdaterAccessRight = new Mutex(), UpdaterAccessLeft = new Mutex();
        /// <summary>
        /// Обновляет левую таблицу
        /// </summary>
        /// <param name="obj"></param>
        protected void UpdateLeft(Object obj)
        {
            //захватываем работу
            UpdaterAccessLeft.WaitOne();
            //Получаем иконки
            if (FileDescriptionLeft.Count > 0)
            {
                for (int i = 0; i < FileDescriptionLeft.Count; i++)
                {
                    if (FileDescriptionLeft[i].FileIcon == null)
                    {
                        FileDescriptionLeft[i].GetFileIcon();
                    }
                }
                //визуальное обновление
                Application.Current.Dispatcher.BeginInvoke((Action)(delegate ()
                {
                    var Selected = LeftFiles.SelectedItem;
                    var a = LeftFiles.Cursor;
                    LeftFiles.Items.Refresh();
                    try
                    {
                        LeftFiles.SelectedItem = Selected;
                        LeftFiles.Cursor = a;
                    }
                    catch (Exception)
                    {//Неудалось присвоить выделенное
                    }
                }), null);
                _iconUpdaterLeft.Change(Timeout.Infinite, Timeout.Infinite);
            }

            //}
            //освобождаем доступ
            UpdaterAccessLeft.ReleaseMutex();
        }
        /// <summary>
        /// Обновляет правую таблицу
        /// </summary>
        /// <param name="obj"></param>
        protected void UpdateRight(Object obj)
        {
            //захватываем работу
            UpdaterAccessRight.WaitOne();

            if (FileDescriptionRight.Count > 0)
            {
                for (int i = 0; i < FileDescriptionRight.Count; i++)
                {
                    if (FileDescriptionRight[i].FileIcon == null)
                    {
                        FileDescriptionRight[i].GetFileIcon();
                    }
                }
                //визуальное обновление
                Application.Current.Dispatcher.BeginInvoke((Action)(delegate ()
                {
                    int i = RightFiles.SelectedIndex;
                    RightFiles.Items.Refresh();
                    RightFiles.SelectedIndex = i;
                }), null);
                _iconUpdaterRight.Change(Timeout.Infinite, Timeout.Infinite);
            }
            //освобождаем доступ
            UpdaterAccessRight.ReleaseMutex();
        }
        #endregion

        #region FilesCollectionUpdate
        //Делегат обновления коллекции файлов
        public UpateFilesList Accept = ChageSignatures;
        public delegate void UpateFilesList(ObservableCollection<FileInformation> Source, ObservableCollection<FileInformation> Dest, Timer IconTimer);
        public static void ChageSignatures(ObservableCollection<FileInformation> Source, ObservableCollection<FileInformation> Dest, Timer IconTimer)
        {
            try
            {
                Dest.Clear();
                if (Source != null)
                {
                    foreach (var x in Source)
                    {
                        Dest.Add(x);
                    }
                    Source.Clear();
                }
                if (IconTimer != null)
                    IconTimer.Change(40, 20);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
        #endregion

        //Делегат проверки законченности копирования, для визуального очищения задачи. (копирования)
        public CheckCopyFunctionList Checker = CheckCopyInformCollection;
        public delegate void CheckCopyFunctionList(ObservableCollection<CopyInform> Source);
        public static void CheckCopyInformCollection(ObservableCollection<CopyInform> Source)
        {
            for (int i = Source.Count - 1; i >= 0; i--)
                if (Source[i].Complete)
                    Source.RemoveAt(i);
        }


        //Drivers enum update
        private void PathLeft_DropDownOpened(object sender, EventArgs e)
        {
            _DriversLeft.UpdateDriversList();
        }

        private void PathRight_DropDownOpened(object sender, EventArgs e)
        {
            _DriversRight.UpdateDriversList();
        }


        //Files list update
        private void UpdateFilesList(FileFinderThread finder)
        {
            try
            {

                if (finder.FilesThread != null && finder.FilesThread.IsAlive)
                {
                    WinAPI.SetEvent(finder.SyncEvent);
                    finder.FilesThread.Join();

                    WinAPI.ResetEvent(finder.SyncEvent);
                }

                finder.FilesThread = new Thread(new ParameterizedThreadStart(FileFinderThread.FileFinderThreadInit));
                finder.FilesThread.Start(finder);
            }
            catch (Exception ex)
            {
                //We can write to log-file this error
                MessageBox.Show(String.Format("{0} is incorrect path! \n {1}", PathLeft.Text, ex.Message), "Error in path", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        //создаем экземпляр класса для мониторинга файлов в папке
        protected FileFinderThread CreateFileFinder(FileFinderThread Current, IntPtr Event, ObservableCollection<FileInformation> Files, String Path, Timer IconUpdater, Thread ThreadHandler = null)
        {
            if (String.IsNullOrWhiteSpace(Path)) throw new Exception("This folder isn't existing!");
            if (Current == null)
                return new FileFinderThread(Event, Files, Path, Accept, IconUpdater, ThreadHandler);
            Current.SyncEvent = Event;
            Current.MonitoringPath = Path;
            Current.FilesList = Files;
            Current.CurrentTimer = IconUpdater;
            if (ThreadHandler != null)
                Current.FilesThread = ThreadHandler;
            return Current;
        }

        //Events of  files list update

        //Изменение пути
        String _previousPathLeft = "";
        private void PathLeftEvent_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_previousPathLeft != "" && PathLeft.Text == "")
            {
                PathLeft.Text = _previousPathLeft;
                return;
            }
            _previousPathLeft = PathLeft.Text;
            UpdateFilesList(_filesLeft = CreateFileFinder(_filesLeft, _eventLeft, FileDescriptionLeft, PathLeft.Text, _iconUpdaterLeft));
        }

        String _previousPathRight;
        private void PathRightEvent_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_previousPathRight != "" && PathRight.Text == "")
            {
                PathRight.Text = _previousPathRight;
                return;
            }
            _previousPathRight = PathRight.Text;
            UpdateFilesList(_filesRight = CreateFileFinder(_filesRight, _eventRight, FileDescriptionRight, PathRight.Text, _iconUpdaterRight));
        }


        //Ожидание завершения потоков
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            if (_filesLeft != null && _filesLeft.FilesThread != null && _filesLeft.FilesThread.IsAlive)
            {
                WinAPI.SetEvent(_filesLeft.SyncEvent);
                _filesLeft.FilesThread.Join();
                WinAPI.CloseHandle(_filesLeft.SyncEvent);
            }
            if (_filesRight != null && _filesRight.FilesThread != null && _filesRight.FilesThread.IsAlive)
            {
                WinAPI.SetEvent(_filesRight.SyncEvent);
                _filesRight.FilesThread.Join();
                WinAPI.CloseHandle(_filesRight.SyncEvent);
            }
            for (int i = 0; i < FilesCopyQueue.Count; i++)
            {
                try
                {
                    FilesCopyQueue[i].WaitThread();
                }
                catch (Exception) { }
            }
        }

        //Открытие файла | папки
        private void ClickEvent(IList Files, ComboBox Path)
        {
            if (Files.Count == 0) return;
            Boolean PathAlreadyChanged = false;

            String StdPath = Path.Text;
            //Копирование коллекции
            ArrayList SelectedFiles = new ArrayList();
            foreach (var x in Files)
                SelectedFiles.Add(x);
            //"Запуск" файлов
            foreach (var Item in SelectedFiles)
            {
                if (!(Item is FileInformation)) return;
                FileInformation Current = Item as FileInformation;
                if (Current.FileSize == "Folder")
                {
                    if ((Current.FileName == ".." || Current.FileName == ".") && !PathAlreadyChanged)
                    {
                        //Идем по папкам назад
                        StringBuilder CurrentPath = new StringBuilder(Path.Text);
                        CurrentPath.Remove(CurrentPath.Length - 1, 1);
                        while (CurrentPath.Length > 0 && CurrentPath[CurrentPath.Length - 1] != '\\' && CurrentPath[CurrentPath.Length - 1] != '/')
                        {
                            CurrentPath.Remove(CurrentPath.Length - 1, 1);
                            if (CurrentPath.Length == 0)
                            {
                                Path.Text = Path.Items[0].ToString();
                            }
                        }
                        Path.Text = CurrentPath.ToString();

                    }
                    else//открываем новую папку
                    {
                        if (!PathAlreadyChanged)
                            Path.Text += Current.FileName + "\\";
                        else
                        {
                            Process newProc = new Process();
                            newProc.StartInfo = new System.Diagnostics.ProcessStartInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
                            newProc.StartInfo.FileName = System.Reflection.Assembly.GetExecutingAssembly().Location;
                            newProc.StartInfo.Arguments = StdPath + Current.FileName + "\\";
                            newProc.Start();

                        }
                    }
                    PathAlreadyChanged = true;
                }
                else
                {
                    try
                    {
                        Process newProc = new Process();
                        newProc.StartInfo = new System.Diagnostics.ProcessStartInfo(Path.Text + Current.FileName);
                        newProc.StartInfo.WorkingDirectory = Path.Text;
                        newProc.Start();
                    }
                    catch (Exception)
                    {

                    }
                }

            }


        }

        //Открытие
        private void LeftFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ClickEvent(LeftFiles.SelectedItems, PathLeft);
        }

        private void RightFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ClickEvent(RightFiles.SelectedItems, PathRight);
        }

        //Вставка клавиатурная
        public void KeyboardPaste(String Path)
        {
            if (Clipboard.ContainsFileDropList())
            {
                var Res = Clipboard.GetFileDropList();
                foreach (var x in Res)
                {
                    String FileName = FileProcessor.ExecuteFileName(x);
                    String FileFolder = FileProcessor.ExecuteFileFolder(x, FileName);
                    try
                    {
                        FilesCopyQueue.Add(new CopyInform(FileFolder, Path, FileName, System.IO.Directory.Exists(x) ? "Folder" : (new FileInfo(x)).Length.ToString(), FilesCopyQueue, Mode.Copy, Checker));
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        #region KeyDownHandler
        //Поиск по словам
        private String _entryString = "";
        /// <summary>
        /// Поиск вхождения нажатой клавиши (навигация) 
        /// Производит поиск файла по введеным символам
        /// </summary>
        /// <param name="currentWindow">ListBox активный</param>
        /// <param name="navChar">Нажатая клавиша</param>
        private void FindEntryString(ListView currentWindow, Char navChar)
        {
            //Ранее производился поиск
            Int32 Position = _entryString.Length;
            if (currentWindow.SelectedIndex != -1)
            {
                for (int i = currentWindow.SelectedIndex; i < currentWindow.Items.Count; i++)
                {
                    //Предыдущие одинаковы?
                    Boolean isEqual = true;
                    for (int j = 0; j < _entryString.Length && isEqual; j++)
                    {
                        if ((currentWindow.Items[i] as FileInformation).FileName.Length > Position &&
                            char.ToLower((currentWindow.Items[i] as FileInformation).FileName[j]) != char.ToLower(_entryString[j]))
                            isEqual = false;
                    }

                    if ((currentWindow.Items[i] as FileInformation).FileName.Length > Position &&
                        char.ToLower((currentWindow.Items[i] as FileInformation).FileName[Position]) == char.ToLower(navChar) &&
                        isEqual)
                    {
                        _entryString += navChar;
                        currentWindow.SelectedIndex = i;
                        return;
                    }
                }
                //Нужного элемента нет, поиск с 0 позиции, но с выделенного элемента
                _entryString = "";
                Position = 0;
                for (int i = currentWindow.SelectedIndex + 1; i < currentWindow.Items.Count; i++)
                {
                    if ((currentWindow.Items[i] as FileInformation).FileName.Length > Position &&
                        char.ToLower((currentWindow.Items[i] as FileInformation).FileName[Position]) == char.ToLower(navChar))
                    {
                        _entryString += navChar;
                        currentWindow.SelectedIndex = i;
                        return;
                    }
                }
            }
            //Нужного элемента нет, либо поиск не производился
            _entryString = "";
            Position = 0;
            for (int i = 0; i < currentWindow.Items.Count; i++)
            {
                if ((currentWindow.Items[i] as FileInformation).FileName.Length > Position &&
                    char.ToLower((currentWindow.Items[i] as FileInformation).FileName[Position]) == char.ToLower(navChar))
                {
                    _entryString += navChar;
                    currentWindow.SelectedIndex = i;
                    return;
                }
            }
        }
        //нажатие клавиш

        /// <summary>
        /// Обработчик нажатия клавиш. Унифицирован
        /// </summary>
        /// <param name="e">Параметры нажатой клавиши</param>
        /// <param name="SelectedIndex">Индекс файла</param>
        /// <param name="CurrentBox">Текущая папка</param>
        /// <param name="DestFolder">Используется при копировании данных - вторая открытая папка</param>
        /// <param name="CurrentFileDescription">Список файлов в текущей папке</param>
        private void KeyDownHandler(ListView CurrentFilesListView, KeyEventArgs e, IList SelectedItems, ComboBox CurrentBox,
            String DestFolder, ObservableCollection<FileInformation> CurrentFileDescription)
        {
            if (Keyboard.Modifiers == ModifierKeys.None)
            {
                FindEntryString(CurrentFilesListView, WinAPI.GetCharFromKey(e.Key));
            }
            //Вставка
            if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                KeyboardPaste(CurrentBox.Text);

            //Add new items to file system (file\folder)
            if (e.Key == Key.D && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                AddFolder_Click(null, null);
            if (e.Key == Key.N && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                AddFile_Click(null, null);

            if (SelectedItems.Count == 0) return;

            //Rename files
            if (e.Key == Key.R && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                RenameFile_Click(null, null);

            //Копирование
            if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                foreach (var x in SelectedItems)
                    FilesCopyQueue.Add(new CopyInform(CurrentBox.Text, DestFolder, (x as FileInformation).FileName,
                        (x as FileInformation).FileSize, FilesCopyQueue, Mode.Copy, Checker));
            }

            //Вход\запуск
            if (e.Key == Key.Enter)
                ClickEvent(SelectedItems, CurrentBox);

            //вставка в буфер обмена
            if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                var x = new System.Collections.Specialized.StringCollection();
                foreach (var item in SelectedItems)
                    x.Add(CurrentBox.Text + (item as FileInformation).FileName);
                Clipboard.SetFileDropList(x);
            }

            //Удаление
            if (e.Key == Key.Delete)
            {
                foreach (var x in SelectedItems)
                {
                    new Thread(new ParameterizedThreadStart(FileProcessor.Remove))
                    .Start(
                    new FileProcessor.RemovingStructure(
                        CurrentBox.Text + (x as FileInformation).FileName,
                        (x as FileInformation)));
                }
            }
        }

        //Обработка нажатий клавиш
        private void LeftFiles_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right || e.Key == Key.Left)
            {
                RightFiles.Focus();
                // Int32 SaveIndex = RightFiles.SelectedIndex;
                RightFiles.Items.Refresh();
                // RightFiles.SelectedIndex = SaveIndex;
            }
            KeyDownHandler(sender as ListView, e, LeftFiles.SelectedItems, PathLeft, PathRight.Text, FileDescriptionLeft);
        }


        private void RightFiles_KeyDown_1(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right || e.Key == Key.Left)
            {
                LeftFiles.Focus();
                //TODO: посмотреть как сохранить индекс
                //Int32 SaveIndex = LeftFiles.SelectedIndex;
                LeftFiles.Items.Refresh();
                //LeftFiles.SelectedIndex = SaveIndex;

            }
            KeyDownHandler(sender as ListView, e, RightFiles.SelectedItems, PathRight, PathLeft.Text, FileDescriptionRight);
        }

        //Получение фокуса
        private void LeftFiles_GotFocus(object sender, RoutedEventArgs e)
        {
            _LastFocusedLeft = true;
        }

        private void RightFiles_GotFocus(object sender, RoutedEventArgs e)
        {
            _LastFocusedLeft = false;
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_LastFocusedLeft)
            {
                if (LeftFiles.SelectedIndex == -1) return;
                WinAPI.DeleteFile(PathLeft.Text + FileDescriptionLeft[LeftFiles.SelectedIndex].FileName);
            }
            else
            {
                if (RightFiles.SelectedIndex == -1) return;
                WinAPI.DeleteFile(PathRight.Text + FileDescriptionRight[RightFiles.SelectedIndex].FileName);
            }
        }
        //Files copy Icon function
        private void CopyFiles_Click(object sender, RoutedEventArgs e)
        {
            if (_LastFocusedLeft)
            {
                if (LeftFiles.SelectedIndex == -1) return;
                FilesCopyQueue.Add(new CopyInform(PathLeft.Text, PathRight.Text, FileDescriptionLeft[LeftFiles.SelectedIndex].FileName,
                    FileDescriptionLeft[LeftFiles.SelectedIndex].FileSize, FilesCopyQueue, Mode.Copy, Checker));
            }
            else
            {
                if (RightFiles.SelectedIndex == -1) return;
                FilesCopyQueue.Add(new CopyInform(PathRight.Text, PathLeft.Text, FileDescriptionRight[RightFiles.SelectedIndex].FileName,
                        FileDescriptionRight[RightFiles.SelectedIndex].FileSize, FilesCopyQueue, Mode.Copy, Checker));
            }

        }


        #endregion

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Window_Closing(null, null);

            Application.Current.Shutdown();
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(@"NavigatorRV program.
It's Simple file navigator.
");
        }

        #region Add\Rename Items methods
        /// <summary>
        /// Call dialog to add new folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            FileNameChanger CreateNewDir = new FileNameChanger("", _LastFocusedLeft ? PathLeft.Text : PathRight.Text, ChangeNameMode.NewFile, FileType.Directory);
            CreateNewDir.ShowDialog();
        }
        /// <summary>
        /// Call dialog to add new file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            FileNameChanger CreateNewFile = new FileNameChanger("", _LastFocusedLeft ? PathLeft.Text : PathRight.Text, ChangeNameMode.NewFile, FileType.File);
            CreateNewFile.ShowDialog();
        }
        /// <summary>
        /// Переименовывание файлов. Используются выделенные файлы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RenameFile_Click(object sender, RoutedEventArgs e)
        {
            if (_LastFocusedLeft)
            {
                //Хранилище (чтобы не было конфликтов)
                ArrayList Selected = new ArrayList();
                foreach (var x in LeftFiles.SelectedItems)
                {
                    Selected.Add(x);
                }
                //Собственно - переименовывание
                foreach (var x in Selected)
                {
                    FileNameChanger CreateNewFile = new FileNameChanger((x as FileInformation).FileName, PathLeft.Text, ChangeNameMode.RenameFile, (x as FileInformation).FileSize == "Folder" ? FileType.Directory : FileType.File);
                    CreateNewFile.ShowDialog();
                }
            }
            else
            {
                ArrayList Selected = new ArrayList();
                foreach (var x in RightFiles.SelectedItems)
                {
                    Selected.Add(x);
                }
                foreach (var x in Selected)
                {
                    FileNameChanger CreateNewFile = new FileNameChanger((x as FileInformation).FileName, PathRight.Text, ChangeNameMode.RenameFile, (x as FileInformation).FileSize == "Folder" ? FileType.Directory : FileType.File);
                    CreateNewFile.ShowDialog();
                }
            }
        }
        #endregion


        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            new SettingsForm().ShowDialog();

            _userSettings.ReadSettings();
            if (_userSettings.isIconShowing)
            {
                _iconUpdaterLeft = new Timer(UpdateLeft, null, Timeout.Infinite, Timeout.Infinite);
                _iconUpdaterRight = new Timer(UpdateRight, null, Timeout.Infinite, Timeout.Infinite);
            }
            //Если параметр - развернутое окно
            if (_userSettings.AllDock)
            {
                WindowState = System.Windows.WindowState.Maximized;
            }
            else WindowState = System.Windows.WindowState.Normal;

        }

        private void MenuItem_Click_4(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(@"You can use next hotkeys:
delete - delete
ctrl + s - copy to another folder
ctrl + c - copy to clipboard
ctrl + v - paste from clipboard
ctrl + n - create new file
ctrl + d - create new folder
ctrl + r - rename file(s)s
arrows up and down - navigation
arrows left and right - turn to another files list
Enter - open
");
        }

        #region Files Drop

        /// <summary>
        /// Added dropped files to copy queue
        /// </summary>
        /// <param name="Files">List of files</param>
        /// <param name="DestPath">Destination folder</param>
        private void FilesDrop(String[] Files, String DestPath)
        {
            foreach (var x in Files)
            {
                //For each file we get his folder and his name.
                String FileName = FileProcessor.ExecuteFileName(x);
                String PathToFileFolder = FileProcessor.ExecuteFileFolder(x, FileName);
                if (System.IO.Directory.Exists(x))
                {//Then added him to Copy Queue
                    //If File is folder
                    FilesCopyQueue.Add(new CopyInform(PathToFileFolder, DestPath, FileName,
                        "Folder", FilesCopyQueue, Mode.Copy, Checker));
                }
                else
                    //if file isn't folder
                    FilesCopyQueue.Add(new CopyInform(PathToFileFolder, DestPath, FileName,
                        FileProcessor.GetUserReadableFileSize((new FileInfo(x)).Length), FilesCopyQueue, Mode.Copy, Checker));
            }
        }
        /// <summary>
        /// Ledt viewer Drop method
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LeftFiles_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && _dragDirection != -1)
            {
                _dragDirection = 0;
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                FilesDrop(files, PathLeft.Text);
            }
        }
        /// <summary>
        /// Right viewer drop method
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RightFiles_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && _dragDirection != 1)
            {
                _dragDirection = 0;
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                FilesDrop(files, PathRight.Text);
            }
        }

        private void LeftFiles_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
        }

        #endregion

        #region Files drag

        Point _startMousePosLeft;
        Point _startMousePosRight;
        //Направление откуда идет drag -1 -лево, 1 право
        Int32 _dragDirection = 0;
        protected void ResetTimer(Object args)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(delegate () {
                _dragDirection = 0;
                _dragResetTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }), null);
        }

        Timer _dragResetTimer = null;

        //Get mouse position for left viewer
        private void LeftFiles_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startMousePosLeft = e.GetPosition(null);

        }
        //Get mouse position for right viewer
        private void RightFiles_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startMousePosRight = e.GetPosition(null);
        }

        /// <summary>
        /// Drag init method
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">Mouse current position and buttons state</param>
        private void LeftFiles_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Vector difference = _startMousePosLeft - e.GetPosition(null);

            if (e.LeftButton == MouseButtonState.Pressed &&
            (Math.Abs(difference.X) > SystemParameters.MinimumHorizontalDragDistance ||
             Math.Abs(difference.Y) > SystemParameters.MinimumVerticalDragDistance)
                && LeftFiles.SelectedIndex >= 0)
            {//Check drag possibility
                String[] Files = new String[LeftFiles.SelectedItems.Count];
                int i = 0;
                foreach (var x in LeftFiles.SelectedItems)
                {
                    Files[i] = PathLeft.Text + ((FileInformation)x).FileName;
                }
                DataObject data = new DataObject(DataFormats.FileDrop, Files, true);
                _dragDirection = -1;
                _dragResetTimer.Change(2000, 2000);
                DragDrop.DoDragDrop(LeftFiles, data, DragDropEffects.All);
            }
        }
        /// <summary>
        /// Drag init method
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">Mouse current position and buttons state</param>
        private void RightFiles_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Vector difference = _startMousePosRight - e.GetPosition(null);
            if (e.LeftButton == MouseButtonState.Pressed &&
            (Math.Abs(difference.X) > SystemParameters.MinimumHorizontalDragDistance ||
             Math.Abs(difference.Y) > SystemParameters.MinimumVerticalDragDistance)
                && RightFiles.SelectedIndex >= 0)
            {
                String[] Files = new String[RightFiles.SelectedItems.Count];
                int i = 0;
                foreach (var x in RightFiles.SelectedItems)
                {
                    Files[i] = PathRight.Text + ((FileInformation)x).FileName;
                }
                DataObject data = new DataObject(DataFormats.FileDrop, Files, true);
                _dragDirection = 1;
                _dragResetTimer.Change(2000, 2000);
                DragDrop.DoDragDrop(RightFiles, data, DragDropEffects.All);
            }
        }
        #endregion

        #region Favorite folder's
        //Создание избранных папок (закладок)

        //Выбор пункта меню
        private void AddToFavoriteClickLeft(object sender, RoutedEventArgs e)
        {
            AddToFavoriteClick(PathLeft, null);
        }
        private void AddToFavoriteClickRight(object sender, RoutedEventArgs e)
        {
            AddToFavoriteClick(PathRight, null);
        }
        //Добавление в избранное
        private void AddToFavoriteClick(object sender, RoutedEventArgs e)
        {
            using (var x = new StreamWriter(Settings.FavoriteFoldersPath, true))
            {
                x.WriteLine((sender as ComboBox).Text);
            }
            _DriversLeft.Add((sender as ComboBox).Text);
            _DriversRight.Add((sender as ComboBox).Text);
        }

        //Вызовы удаления
        private void DeleteFavLeft(object sender, RoutedEventArgs e)
        {
            DeleteFromFavorite(PathLeft, null);
        }
        private void DeleteFavRight(object sender, RoutedEventArgs e)
        {
            DeleteFromFavorite(PathRight, null);
        }
        /// <summary>
        /// удаляет ненужную более закладку пути 
        /// </summary>
        /// <param name="sender">Combobox с которого необходимо работать</param>
        /// <param name="e"></param>
        private void DeleteFromFavorite(object sender, RoutedEventArgs e)
        {
            Boolean isFavoritePathExists = false;
            foreach (var x in PathLeft.Items)
            {
                if (x.ToString() == (sender as ComboBox).Text)
                    isFavoritePathExists = true;

            }
            if (isFavoritePathExists)
            {
                using (var Writer = new StreamWriter(Settings.FavoriteFoldersPath + "~", true))
                {
                    using (var Reader = new StreamReader(Settings.FavoriteFoldersPath))
                    {
                        while (!Reader.EndOfStream)
                        {
                            String TempPath = Reader.ReadLine();
                            if (TempPath != (sender as ComboBox).Text)
                                Writer.WriteLine(TempPath);
                        }
                    }
                }
                File.Delete(Settings.FavoriteFoldersPath);
                File.Move(Settings.FavoriteFoldersPath + "~", Settings.FavoriteFoldersPath);
            }

        }
        #endregion



        private void FtpConnect_Click(object sender, RoutedEventArgs e)
        {
            FtpConnectWindow ftpConnect = new FtpConnectWindow();
            ftpConnect.Show();
        }

        private void ZipFile_Click(object sender, RoutedEventArgs e)
        {
            if (_LastFocusedLeft)
            {
                if (LeftFiles.SelectedIndex == -1) return;
                if (FileDescriptionLeft[LeftFiles.SelectedIndex].FileType != ".") 
                { 
                    MessageBox.Show("К сожалению, вы можете архивировать только папку");
                    return; 
                }
                string zipPath = PathLeft.Text + FileDescriptionLeft[LeftFiles.SelectedIndex].FileName;
                string extractPath = PathRight.Text + FileDescriptionLeft[LeftFiles.SelectedIndex].FileName + ".zip";
                ZipFile.CreateFromDirectory(zipPath, extractPath);
            }
            else
            {
                if (RightFiles.SelectedIndex == -1) return;
                if (FileDescriptionLeft[LeftFiles.SelectedIndex].FileType != ".")
                {
                    MessageBox.Show("К сожалению, вы можете архивировать только папку");
                    return;
                }
                string zipPath = PathRight.Text + FileDescriptionLeft[RightFiles.SelectedIndex].FileName;
                string extractPath = PathLeft.Text + FileDescriptionLeft[LeftFiles.SelectedIndex].FileName + ".zip";
                ZipFile.CreateFromDirectory(zipPath, extractPath);
            }
        }
        private void OpenNotepad_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("notepad");
        }

        private void OpenCalculator_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("calc");
        }

        private void ResetExplorer_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer");
        }

        private void UnzipFile_Click(object sender, RoutedEventArgs e)
        {
            if (_LastFocusedLeft)
            {
                if (LeftFiles.SelectedIndex == -1) return;
                string zipPath = PathLeft.Text + FileDescriptionLeft[LeftFiles.SelectedIndex].FileName;
                string extractPath = PathRight.Text;
                ZipFile.ExtractToDirectory(zipPath, extractPath);
            }
            else
            {
                if (RightFiles.SelectedIndex == -1) return;
                string zipPath = PathRight.Text + FileDescriptionLeft[RightFiles.SelectedIndex].FileName;
                string extractPath = PathLeft.Text;
                ZipFile.ExtractToDirectory(zipPath, extractPath);
            }
        }

        //static void Main(string[] args)
        //{
        //    string sourceFile = "D://test/book.pdf"; // исходный файл
        //    string compressedFile = "D://test/book.gz"; // сжатый файл
        //    string targetFile = "D://test/book_new.pdf"; // восстановленный файл

        //    // создание сжатого файла
        //    Compress(sourceFile, compressedFile);
        //    // чтение из сжатого файла
        //    Decompress(compressedFile, targetFile);

        //    Console.ReadLine();
        //}
        //public static void Compress(string sourceFile, string compressedFile)
        //{
        //    // поток для чтения исходного файла
        //    using (FileStream sourceStream = new FileStream(sourceFile, FileMode.OpenOrCreate))
        //    {
        //        // поток для записи сжатого файла
        //        using (FileStream targetStream = File.Create(compressedFile))
        //        {
        //            // поток архивации
        //            using (GZipStream compressionStream = new GZipStream(targetStream, CompressionMode.Compress))
        //            {
        //                sourceStream.CopyTo(compressionStream); // копируем байты из одного потока в другой
        //                Console.WriteLine("Сжатие файла {0} завершено. Исходный размер: {1}  сжатый размер: {2}.",
        //                    sourceFile, sourceStream.Length.ToString(), targetStream.Length.ToString());
        //            }
        //        }
        //    }
        //}
        //public static void Decompress(string compressedFile, string targetFile)
        //{
        //    // поток для чтения из сжатого файла
        //    using (FileStream sourceStream = new FileStream(compressedFile, FileMode.OpenOrCreate))
        //    {
        //        // поток для записи восстановленного файла
        //        using (FileStream targetStream = File.Create(targetFile))
        //        {
        //            // поток разархивации
        //            using (GZipStream decompressionStream = new GZipStream(sourceStream, CompressionMode.Decompress))
        //            {
        //                decompressionStream.CopyTo(targetStream);
        //                Console.WriteLine("Восстановлен файл: {0}", targetFile);
        //            }
        //        }
        //    }
        //}
        private void LeftFiles_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {

            ShellContextMenu scm = new ShellContextMenu();
            List<FileInformation> items = new List<FileInformation>();
            foreach (var x in LeftFiles.SelectedItems)
            {
                items.Add(((FileInformation)x));
            }

            if (items.Count > 0 && items[0].FileType == ".")
            {
                DirectoryInfo[] directories = new DirectoryInfo[1];
                directories[0] = new DirectoryInfo(PathLeft.Text + items[0].FileName);
                scm.ShowContextMenu(new WindowInteropHelper(this).Handle, directories, (int)e.GetPosition(null).X, (int)e.GetPosition(null).Y);
                return;
            }

            FileInfo[] files = new FileInfo[1];
            if (items.Count > 0)
                files[0] = new FileInfo(PathLeft.Text + items[0].FileName);
            else files = new FileInfo[0];

            scm.ShowContextMenu(new WindowInteropHelper(this).Handle, files, (int)e.GetPosition(null).X, (int)e.GetPosition(null).Y);

        }

        private void RightFiles_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {

            ShellContextMenu scm = new ShellContextMenu();
            List<FileInformation> items = new List<FileInformation>();
            foreach (var x in RightFiles.SelectedItems)
            {
                items.Add(((FileInformation)x));
            }

            if (items.Count > 0 && items[0].FileType == ".")
            {
                DirectoryInfo[] directories = new DirectoryInfo[1];
                directories[0] = new DirectoryInfo(PathRight.Text + items[0].FileName);
                scm.ShowContextMenu(new WindowInteropHelper(this).Handle, directories, (int)e.GetPosition(null).X, (int)e.GetPosition(null).Y);
                return;
            }

            FileInfo[] files = new FileInfo[1];
            if (items.Count > 0)
                files[0] = new FileInfo(PathRight.Text + items[0].FileName);
            else files = new FileInfo[0];

            scm.ShowContextMenu(new WindowInteropHelper(this).Handle, files, (int)e.GetPosition(null).X, (int)e.GetPosition(null).Y);

        }
    }
}

