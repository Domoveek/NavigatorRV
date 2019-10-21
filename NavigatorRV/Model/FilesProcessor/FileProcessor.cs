using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NavigatorRV.Model
{
    /// <summary>
    /// Класс обработки файлов (статические методы работы с файлами)
    /// </summary>
    class FileProcessor
    {
        #region Execution Functions
        static public String ExecuteFileFolder(String FullFilePath, String CurrentFileName)
        {
            return FullFilePath.Remove(FullFilePath.Length - CurrentFileName.Length);
        }

        static public String ExecuteFileName(String FullFilePath)
        {
            StringBuilder a = new StringBuilder();
            int i = FullFilePath.Length - 1;
            for (; i >= 0 && FullFilePath[i] != '\\' && FullFilePath[i] != '/'; i--) ;

            for (int j = i + 1; j < FullFilePath.Length; j++)
                a.Append(FullFilePath[j]);

            return a.ToString();

        }

        static public String ExecuteFileType(String FileName)
        {
            try
            {
                StringBuilder a = new StringBuilder();
                int i;
                for (i = FileName.Length - 1; i >= 0 && FileName[i] != '.'; i--) ;
                if (FileName[i] == '.')
                    for (i = i + 1; i < FileName.Length; i++)
                    {
                        a.Append(FileName[i]);
                    }

                if (a.Length > 0)
                    return a.ToString();
                return "";
            }
            catch (Exception /*e*/)
            {
                return "";
            }

        }
        #endregion


        #region FileSizeConverter
        /// <summary>
        /// Calculate user-frendly file size string
        /// </summary>
        /// <param name="FSize">Current file size in bytes</param>
        /// <returns>File size string</returns>
        public static String GetUserReadableFileSize(UInt64 FSize)
        {
            String FileSize;
            //calculate file size string
            if (FSize == 0)
                FileSize = "0";
            else
                if (FSize < 1024)
                FileSize = FSize.ToString() + " Bytes";
            else if (FSize < 1024 * 1024)
                FileSize = (FSize / (double)1024).ToString("F") + " KB";
            else if (FSize < 1024 * 1024 * 1024)
                FileSize = (FSize / (double)(1024 * 1024)).ToString("F") + " MB";
            else FileSize = (FSize / (double)(1024 * 1024 * 1024)).ToString("F") + " GB";

            return FileSize;
        }

        public static String GetUserReadableFileSize(long FSize)
        {
            String FileSize;
            //calculate file size string
            if (FSize == 0)
                FileSize = "0";
            else
                if (FSize < 1024)
                FileSize = FSize.ToString() + " Bytes";
            else if (FSize < 1024 * 1024)
                FileSize = (FSize / (double)1024).ToString("F") + " KB";
            else if (FSize < 1024 * 1024 * 1024)
                FileSize = (FSize / (double)(1024 * 1024)).ToString("F") + " MB";
            else FileSize = (FSize / (double)(1024 * 1024 * 1024)).ToString("F") + " GB";

            return FileSize;
        }
        #endregion
        public struct RemovingStructure
        {
            public String Path;
            public FileInformation File;

            public RemovingStructure(String path, FileInformation file)
            {
                Path = path;
                File = file;
            }
        }

        static public void Remove(Object obj)
        {
            if (!(obj is RemovingStructure)) return;
            try
            {
                RemovingStructure CurrentRemoving = (RemovingStructure)obj;
                if (CurrentRemoving.File.FileSize == "Folder" && CurrentRemoving.File.FileName != ".." && CurrentRemoving.File.FileName != ".")
                //RemoveDir(CurrentRemoving.Path);
                {
                    FileSystemMutex.Instance.Enter((Action)(() =>
                    {
                        System.IO.Directory.Delete(CurrentRemoving.Path, true);
                    }));

                }
                else
                    WinAPI.DeleteFile(CurrentRemoving.Path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
