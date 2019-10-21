using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace NavigatorRV.Model
{
    public class Settings
    {
        public Boolean AllDock { get; set; }
        public Boolean isIconShowing { get; set; }
        public static String PathToThisFolderApp = FileProcessor.ExecuteFileFolder(System.Reflection.Assembly.GetExecutingAssembly().Location,
              FileProcessor.ExecuteFileName(System.Reflection.Assembly.GetExecutingAssembly().Location));
        //Путь к настройкам
        protected String _path = PathToThisFolderApp + "Settings.xml";
        public static readonly String FavoriteFoldersPath = PathToThisFolderApp + "Favorite.txt";

        /// <summary>
        /// Сохраняем изменения в файл
        /// </summary>
        /// <param name="isAllDock"></param>
        /// <param name="isIconsShowing"></param>
        public void AcceptSettings(Boolean isAllDock, Boolean isIconsShowing)
        {
            this.AllDock = isAllDock;
            this.isIconShowing = isIconShowing;
            using (var Writer = new XmlTextWriter(_path, Encoding.UTF8))
            {
                Writer.Formatting = Formatting.Indented;
                Writer.WriteStartElement("Settings");

                Writer.WriteStartElement("AllDock");
                Writer.WriteAttributeString("Checked", isAllDock.ToString());
                Writer.WriteEndElement();

                Writer.WriteStartElement("IconsShower");
                Writer.WriteAttributeString("Checked", isIconsShowing.ToString());
                Writer.WriteEndElement();

                Writer.WriteEndElement();
            }

        }

        public Settings()
        {
            ReadSettings();
        }
        public void ReadSettings()
        {
            if (System.IO.File.Exists(_path))
            {
                using (var xmlReader = new XmlTextReader(_path))
                {
                    while (xmlReader.Read())
                    {
                        if (xmlReader.NodeType == XmlNodeType.Element)
                        {
                            if (xmlReader.Name == "AllDock")
                            {
                                if (xmlReader.MoveToAttribute("Checked"))
                                {
                                    AllDock = Boolean.Parse(xmlReader.Value);
                                }
                            }
                            else if (xmlReader.Name == "IconsShower")
                            {
                                if (xmlReader.MoveToAttribute("Checked"))
                                {
                                    isIconShowing = Boolean.Parse(xmlReader.Value);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
