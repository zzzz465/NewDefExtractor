using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace NewDefExtractor
{
    class Program
    {
        static int NodeLimitPerXMLFile = 40;
        [STAThread]
        static void Main(string[] args)
        {
            string ConfigJsonData = string.Empty;
            SimpleLog.Write("인터넷에서 Config Data를 가져올까요? Y/N : ", ConsoleColor.Cyan);
            if (Console.ReadLine().ToLower() == "y")
            {
                WebClient web = new WebClient();
                byte[] rawData = web.DownloadData(@"https://raw.githubusercontent.com/zzzz465/DefExtractorDB/master/DefExtractorJsonData.json");
                ConfigJsonData = Encoding.UTF8.GetString(rawData);
            }
            else
            {
                SimpleLog.WriteLine("Config data를 선택해주세요.", ConsoleColor.Cyan);
                Thread.Sleep(100);
                OpenFileDialog configPath = new OpenFileDialog();
                configPath.ShowDialog();
                ConfigJsonData = File.ReadAllText(configPath.FileName);
            }

            SimpleLog.WriteLine("Def 폴더를 선택해주세요", ConsoleColor.Green);
            CommonOpenFileDialog defFolder = new CommonOpenFileDialog();
            defFolder.IsFolderPicker = true;
            defFolder.ShowDialog();
            SimpleLog.WriteLine("Def 경로 : " + defFolder.FileName, ConsoleColor.White);

            SimpleLog.WriteLine("추출 경로를 선택해주세요", ConsoleColor.Green);
            CommonOpenFileDialog export = new CommonOpenFileDialog();
            export.IsFolderPicker = true;
            export.ShowDialog();
            SimpleLog.WriteLine("추출 경로 : " + export.FileName, ConsoleColor.White);

            List<ConfigData> configdatas = ConfigData.Load(ConfigJsonData);
            List<FileInfo> xmlFiles = new List<FileInfo>();
            Directory.GetFiles(defFolder.FileName, "*.xml", SearchOption.AllDirectories).ToList().ForEach(item => xmlFiles.Add(new FileInfo(item)));
            ConfigDataController controller = new ConfigDataController(configdatas, xmlFiles.ToArray(), new DirectoryInfo(export.FileName));
            controller.ParseDefFiles();
            Dictionary<string, Dictionary<string, List<TargetNode>>> sorted = controller.GetTargetNodesSorted();
            List<TargetNode> nodes = new List<TargetNode>();
            string latestRootNodeName = string.Empty;
            string latestDefName = string.Empty;

            foreach(string rootName in sorted.Keys)
            {
                int count = 0;
                string folderPath = Path.Combine(export.FileName, rootName);
                string filePath = Path.Combine(folderPath, rootName + count + ".xml");
                Directory.CreateDirectory(folderPath);
                List<TargetNode> nodes2 = new List<TargetNode>();
                
                foreach (var item in sorted[rootName])
                {
                    string defName = item.Key;
                    nodes2.AddRange(item.Value);
                    if (nodes2.Count > NodeLimitPerXMLFile)
                    {
                        nodes2.Sort();
                        XDocument doc = DocumentBuilder.PrepareXDoc(nodes2);
                        doc.Save(Path.Combine(folderPath, rootName + count + ".xml"));
                        count++;
                        nodes2.Clear();
                    }
                }
                if(nodes2.Count > 0)
                {
                    nodes2.Sort();
                    XDocument doc = DocumentBuilder.PrepareXDoc(nodes2);
                    doc.Save(Path.Combine(folderPath, rootName + count + ".xml"));
                    count++;
                    nodes2.Clear();
                }
            }

            SimpleLog.WriteLine("작업이 종료되었습니다! X키를 눌러 창을 닫아주세요.", ConsoleColor.DarkCyan);
            while(true)
            {

            }
        }
    }
}
