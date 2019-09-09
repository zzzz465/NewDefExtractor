using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;
using System.Xml;
using System.Xml.XPath;

namespace NewDefExtractor.GUI
{
    /// <summary>
    /// define single mod element/folder, contain various informations.
    /// </summary>
    public class Mod
    {
        public string ModName { get; }
        public bool isWorkshop { get; } = false;
        public string FolderName { get; }
        public string FolderPath { get; }
        List<string> supportedVersions = new List<string>();
        public IEnumerable<string> GetSupportedVersions { get { return supportedVersions.AsEnumerable(); } }
        public string DefPath { get { return Path.Combine(FolderPath, "Defs"); } }
        public string PreviewImgPath { get { return Path.Combine(FolderPath, "About", "preview.png"); } }
        List<string> LanguageList { get; }
        XDocument AboutXDoc { get; }

        private Mod(DirectoryInfo ModRootFolder)
        {
            //기초 데이터 설정
            FolderPath = ModRootFolder.FullName;
            FolderName = ModRootFolder.Name;
            if (ModRootFolder.FullName.Split('\\').Contains("294100"))
                isWorkshop = true;

            //Xml 설정 및 모드이름 설정, 설명도 추가할까?
            AboutXDoc = XDocument.Load(Path.Combine(FolderPath, "About", "About.xml"));
            ModName = AboutXDoc.XPathSelectElement("//ModMetaData/name").Value;

            //버전 추가
            foreach (XElement elem in AboutXDoc.XPathSelectElements("//supportedVersions/li"))
                supportedVersions.Add(elem.Value);
        }

        /// <summary>
        /// <br>create a single mod instance for target mod folder.</br>
        /// <br>if the mod folder is already loaded or the structure is broken, it will return false. w</br>
        /// </summary>
        /// <param name="ModRootFolder">target mod folder root directory</param>
        /// <returns></returns>
        public static bool LoadSingleMod(DirectoryInfo ModRootFolder, out string message)
        {
            try
            {
                Mod inst = new Mod(ModRootFolder);
                message = null;
                return true;
            }
            catch(Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

    }
}
