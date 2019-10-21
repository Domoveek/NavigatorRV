using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.IO;

namespace NavigatorRV.Model
{
    public class Drivers : ObservableCollection<String>
    {
        public Drivers()
        {

            UpdateDriversList();
        }

        public void UpdateDriversList()
        {
            Char[] Buffer = new Char[126];
            for (int i = 0; i < Buffer.Length; i++)
                Buffer[i] = '0';
            WinAPI.GetLogicalDriveStrings(126, Buffer);
            Clear();
            for (int i = 0; Buffer[i] != '\0' && i < Buffer.Length; i += 4)
            {

                Add(String.Format("{0}:\\", Buffer[i]));
            }
            Add(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\");

            GetFavoriteFolders();
        }
        //Получение списка избранных путей
        private void GetFavoriteFolders()
        {
            if (System.IO.File.Exists(Settings.FavoriteFoldersPath))
                using (var x = new StreamReader(Settings.FavoriteFoldersPath))
                {
                    while (!x.EndOfStream)
                    {
                        String favoritePath = x.ReadLine();
                        Add(favoritePath);
                    }
                }
        }
    }
}
