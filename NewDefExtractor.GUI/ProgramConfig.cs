using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace NewDefExtractor.GUI
{
    public class ProgramConfig
    {
        public static ProgramConfig instance = new ProgramConfig();
        public readonly string ConfigFolderPath = Path.Combine(Environment.GetEnvironmentVariable("APPDATA"), "Madeline");
        public string ConfigFilePath { get { return Path.Combine(ConfigFolderPath, "DefExtractor.json"); } }

        public string WorkshopFolderPath { get { return ConfigXdoc.Root.Element("WorkshopFolderPath").Value; }
                                           set { ConfigXdoc.Root.Element("WorkshopFolderPath").Value = value; ConfigXdoc.Save(ConfigFilePath); } }
        public string LocalFolderPath { get { return ConfigXdoc.Root.Element("LocalFolderPath").Value; }
                                        set { ConfigXdoc.Root.Element("LocalFolderPath").Value = value; ConfigXdoc.Save(ConfigFilePath); } }
        public string ExportFolderPath { get { return ConfigXdoc.Root.Element("ExportFolderPath").Value; }
                                         set { ConfigXdoc.Root.Element("ExportFolderPath").Value = value; ConfigXdoc.Save(ConfigFilePath); } }
        XDocument ConfigXdoc { get; }
		public bool isHaveProperPath
		{
			get
			{
				if (!string.IsNullOrEmpty(LocalFolderPath) && !string.IsNullOrEmpty(ExportFolderPath))
					return true;
				else
					return false;
			}
		}
        private ProgramConfig()
        {
            if (!Directory.Exists(ConfigFolderPath))
                Directory.CreateDirectory(ConfigFolderPath);

            if (!File.Exists(ConfigFilePath)) //처음 컨픽을 생성하는 것이라면
            {
                ConfigXdoc = new XDocument();
                ConfigXdoc.Add(new XElement("Config"));
                ConfigXdoc.Root.Add(
                    new XElement("WorkshopFolderPath"),
                    new XElement("LocalFolderPath"),
                    new XElement("ExportFolderPath")
                    );
                ConfigXdoc.Save(ConfigFilePath);
            }

            ConfigXdoc = XDocument.Load(ConfigFilePath);
        }

		public static void Init()
		{
			//ProgramConfig.instance = new ProgramConfig();
		}
    }
}
