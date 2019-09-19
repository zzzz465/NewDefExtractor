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

            SimpleLog.WriteLine("Select Def or Patch Folder. multiselectable...", ConsoleColor.Green);
            CommonOpenFileDialog TargetFolderPicker = new CommonOpenFileDialog();
            TargetFolderPicker.IsFolderPicker = true;
            TargetFolderPicker.Multiselect = true;
            TargetFolderPicker.ShowDialog();
            SimpleLog.WriteLine("Selected Paths : " + string.Join(" | ", TargetFolderPicker.FileNames), ConsoleColor.White);

            SimpleLog.WriteLine("추출 경로를 선택해주세요", ConsoleColor.Green);
            CommonOpenFileDialog export = new CommonOpenFileDialog();
            export.IsFolderPicker = true;
            export.ShowDialog();
            SimpleLog.WriteLine("추출 경로 : " + export.FileName, ConsoleColor.White);

            List<ConfigData> configdatas = ConfigData.Load(ConfigJsonData);
            foreach (string path in TargetFolderPicker.FileNames)
            {
                List<FileInfo> xmlFiles = new List<FileInfo>();
                Directory.GetFiles(path, "*.xml", SearchOption.AllDirectories).ToList().ForEach(item => xmlFiles.Add(new FileInfo(item)));
                ConfigDataController controller = new ConfigDataController(configdatas, xmlFiles.ToArray(), new DirectoryInfo(export.FileName));
                controller.ParseDefFiles();
                Dictionary<string, Dictionary<string, List<TargetNode>>> sorted = controller.GetTargetNodesSorted();
                List<TargetNode> nodes = new List<TargetNode>();
                string latestRootNodeName = string.Empty;
                string latestDefName = string.Empty;
                int total_nodeCount = 0;

                foreach (string rootName in sorted.Keys)
                {
                    SimpleLog.WriteLine("");
                    SimpleLog.WriteLine(string.Format("상위 노드 {0}에 대한 번역 데이터를 기록합니다...", rootName), ConsoleColor.Cyan);
                    int count = 0;
                    int nodeCount = 0;
                    string folderPath = Path.Combine(export.FileName, rootName);
                    string filePath = Path.Combine(folderPath, rootName + count + ".xml");
                    string defName = string.Empty;
                    Directory.CreateDirectory(folderPath);
                    List<TargetNode> nodes2 = new List<TargetNode>();

                    foreach (var item in sorted[rootName])
                    {
                        defName = item.Key;
                        SimpleLog.Write(string.Format("DefName "), ConsoleColor.Green);
                        SimpleLog.Write(defName, ConsoleColor.White);
                        SimpleLog.WriteLine(" 하위의 번역 데이터를 기록합니다...", ConsoleColor.Green);
                        //여기를 수정했음
                        List<TargetNode> TargetNodesToAdd = item.Value;
                        TargetNodesToAdd.Sort();
                        nodes2.AddRange(TargetNodesToAdd);
                        nodeCount += item.Value.Count;
                        total_nodeCount += item.Value.Count;
                        if (nodes2.Count > NodeLimitPerXMLFile)
                        {
                            XDocument doc = DocumentBuilder.PrepareXDoc(nodes2);
                            doc.Save(Path.Combine(folderPath, rootName + count + ".xml"));
                            count++;
                            nodes2.Clear();
                        }
                    }
                    if (nodes2.Count > 0)
                    {
                        XDocument doc = DocumentBuilder.PrepareXDoc(nodes2);
                        SimpleLog.Write(string.Format("DefName "), ConsoleColor.Green);
                        SimpleLog.Write(defName, ConsoleColor.White);
                        SimpleLog.WriteLine(" 하위의 번역 데이터를 기록합니다...", ConsoleColor.Green);
                        doc.Save(Path.Combine(folderPath, rootName + count + ".xml"));
                        count++;
                        nodes2.Clear();
                    }
                    SimpleLog.Write("상위 노드 ", ConsoleColor.Cyan);
                    SimpleLog.Write(rootName);
                    SimpleLog.Write(" 하위에 ", ConsoleColor.Cyan);
                    SimpleLog.Write(string.Format("{0} 개의 파일 ", count), ConsoleColor.Yellow);
                    SimpleLog.Write(string.Format("{0}개의 노드", nodeCount), ConsoleColor.Red);
                    SimpleLog.WriteLine("가 기록되었습니다.", ConsoleColor.Cyan);
                }

                SimpleLog.WriteLine(string.Format("\n노드 {0} 개에 대한 데이터 작성이 완료되었습니다...", total_nodeCount), ConsoleColor.Cyan);
            }
            SimpleLog.WriteLine("\n\n작업이 종료되었습니다! X키를 눌러 창을 닫아주세요.", ConsoleColor.DarkYellow);
            while (true)
            { }
        }
    }
}
