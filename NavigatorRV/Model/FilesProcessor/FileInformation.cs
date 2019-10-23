using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using System.Windows.Interop;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Reflection;
using System.IO;
using System.Text;
using System.Windows;

namespace NavigatorRV.Model
{
    public class FileInformation
    {
        #region ClassItems


        public ImageSource FileIcon { get; set; }
        public String FileName { get; set; }
        public String FileSize { get; set; }
        public String FileType { get; set; }
        public String LastChangeDate { get; set; }
        private String _fullFileName;


        public FileInformation() { }
        public FileInformation(String FileName, String FileSize, String FileType, String FullFileName, String LastChangeDate)
        {

            this.FileName = FileName;
            this.FileSize = FileSize;
            this.FileType = FileType;
            this.LastChangeDate = LastChangeDate;
            _fullFileName = FullFileName;
            //   GetFileIcon();
        }

        public void GetFileIcon()
        {
            try
            {
                if (System.IO.File.Exists(_fullFileName))
                {
                    using (Icon CurrentIcon = Icon.ExtractAssociatedIcon(_fullFileName))
                    {
                        FileIcon = Imaging.CreateBitmapSourceFromHIcon(CurrentIcon.Handle,
                                System.Windows.Int32Rect.Empty,
                                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                        FileIcon.Freeze();

                    }
                }
                else
                    if ((System.IO.Directory.Exists(_fullFileName)))
                {
                    BitmapImage Bi = null;
                    Bi = new BitmapImage(new Uri(@"pack://application:,,,/NavigatorRV;component/Images/folder.png", UriKind.Absolute));
                    FileIcon = Bi;
                    FileIcon.Freeze();

                    //FileIcon = new BitmapImage(new Uri(PngFolderPath + "\\img\\folder.png"));
                    //FileIcon.Freeze();
                }
                else FileIcon = null;
            }
            catch (Exception)
            {
            }

        }

        #endregion
    }

}
