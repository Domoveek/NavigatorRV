using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NavigatorRV
{
    public enum ChangeNameMode
    {
        NewFile = 0,
        RenameFile = 1
    }

    public enum FileType
    {
        File = 0,
        Directory = 1
    }

    /// <summary>
    /// Change File Name Form
    /// </summary>
    public partial class FileNameChanger : Window
    {
        ChangeNameMode CurrentMode;
        String FolderPath;
        String OldName;
        FileType CurrentFileType;

        public FileNameChanger(String OldName, String FolderPath, ChangeNameMode CurrentMode, FileType CurrentFileType)
        {
            this.FolderPath = FolderPath;
            this.OldName = OldName;
            this.CurrentMode = CurrentMode;
            this.CurrentFileType = CurrentFileType;
            InitializeComponent();
            NewName.Text = OldName;
            this.NewName.Focus();
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                switch (CurrentMode)
                {
                    case ChangeNameMode.RenameFile:
                        {
                            if (CurrentFileType == FileType.File)
                            {
                                if (System.IO.File.Exists(FolderPath + NewName.Text))
                                {
                                    MessageBox.Show("File already exists! Please input another file's name", "Can't rename file", MessageBoxButton.OK, MessageBoxImage.Stop);
                                    break;
                                }
                                System.IO.File.Move(FolderPath + OldName, FolderPath + NewName.Text);
                            }
                            else
                            {
                                if (System.IO.Directory.Exists(FolderPath + NewName.Text))
                                {
                                    MessageBox.Show("Directory already exists! Please input another name of directory", "Can't rename directory", MessageBoxButton.OK, MessageBoxImage.Stop);
                                    break;
                                }
                                System.IO.Directory.Move(FolderPath + OldName, FolderPath + NewName.Text);
                            }
                            break;
                        }
                    case ChangeNameMode.NewFile:
                        {
                            if (CurrentFileType == FileType.File)
                            {
                                if (System.IO.File.Exists(FolderPath + NewName.Text))
                                {
                                    MessageBox.Show("File already exists! Please input another file's name", "Can't create new file", MessageBoxButton.OK, MessageBoxImage.Stop);
                                    return;
                                }
                                var x = System.IO.File.Create(FolderPath + NewName.Text);
                                x.Close();
                                break;
                            }
                            else
                            {
                                if (System.IO.Directory.Exists(FolderPath + NewName.Text))
                                {
                                    MessageBox.Show("Directory already exists! Please input another name of directory", "Can't create directory", MessageBoxButton.OK, MessageBoxImage.Stop);
                                    break;
                                }
                                System.IO.Directory.CreateDirectory(FolderPath + NewName.Text);
                            }
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                Close();
            }
        }

        private void NewName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Accept_Click(sender, null);
            }
        }

    }
}

