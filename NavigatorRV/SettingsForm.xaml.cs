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
using System.Xml;
using System.IO;
using NavigatorRV.Model;

namespace NavigatorRV
{
    /// <summary>
    /// Interaction logic for SettingsForm.xaml
    /// </summary>
    public partial class SettingsForm : Window
    {
        //Получение настроек программы
        private void GetSettings()
        {
            AllDock.IsChecked = _programmSettings.AllDock;
            Icons.IsChecked = _programmSettings.isIconShowing;
        }

        private Settings _programmSettings;
        public SettingsForm()
        {
            InitializeComponent();
            _programmSettings = new Settings();
            GetSettings();
        }

        private void SetSaver_Click(object sender, RoutedEventArgs e)
        {
            _programmSettings.AcceptSettings(AllDock.IsChecked.Value, Icons.IsChecked.Value);
            Close();
        }
    }
}
