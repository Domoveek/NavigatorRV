using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Collections.ObjectModel;
using System.Windows;

namespace NavigatorRV.Model
{
    /// <summary>
    /// Класс поиска файлов
    /// </summary>
    public class FileFinder
    {

        // Путь в файловой системе
        public String MonitoringPath { get; set; }


        public FileFinder(String Path)
        {
            MonitoringPath = Path;
        }

        /// <summary>
        /// Поиск всех папок в директории с заданным селектором
        /// </summary>
        /// <param name="SelectorFiles">Путь + селектор</param>
        /// <param name="Path">Селектор</param>
        /// <param name="IsDirectory">Метка поиска директории</param>
        /// <returns>Список файлов</returns>
        protected IEnumerable<FileInformation> FindFilesCollection(String SelectorFiles, String Path, Boolean IsDirectory)
        {
            WinAPI.WIN32_FIND_DATA NewFile;
            IntPtr FindHandle = WinAPI.FindFirstFile(SelectorFiles, out NewFile);

            if (FindHandle == WinAPI.INVALID_HANDLE_VALUE)
            {
                throw new ArgumentNullException("Can't execute FindFirstFile function! HANDLE is INVALID_HANDLE_VALUE");
            }
            try
            {
                do
                {
                    if ((NewFile.FileAttr & FileAttributes.Directory) == FileAttributes.Directory && IsDirectory)
                        yield return new FileInformation(NewFile.FileName, "Folder", ".", ".", Directory.GetLastWriteTime(Path + NewFile.FileName).ToString());

                    if ((NewFile.FileAttr & FileAttributes.Directory) != FileAttributes.Directory && !IsDirectory)
                    {
                        UInt64 FSize = ((UInt64)((((UInt64)(NewFile.FileSizeHight)) * ((UInt64)(Math.Pow(2, 32))))) + (UInt64)(NewFile.FileSizeLow));
                        String FileSize = FileProcessor.GetUserReadableFileSize(FSize);

                        yield return new FileInformation(NewFile.FileName, FileSize, FileProcessor.ExecuteFileType(NewFile.FileName), Path + NewFile.FileName, File.GetLastWriteTime(Path + NewFile.FileName).ToString());
                    }
                } while (WinAPI.FindNextFile(FindHandle, out NewFile));

            }
            finally
            {
                WinAPI.FindClose(FindHandle);
            }
        }

        //files finder
        public IEnumerable<FileInformation> FilesCollection(String Path)
        {
            String Store = Path;
            if (Path[Path.Length - 1] != '*')
                Path += "*";

            foreach (var x in FindFilesCollection(Path, Store, true))
                yield return x;
            foreach (var x in FindFilesCollection(Path, Store, false))
                yield return x;
        }



    }
}
