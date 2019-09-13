using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Xml.XPath;

namespace NewDefExtractor
{
    public class ConfigDataController
    {
        DirectoryInfo ExportDir;
        List<ConfigData> ConfigDatas { get; set; }
        List<DefXML> defXMLs = new List<DefXML>();
        Dictionary<DefXML, List<TargetNode>> TargetNodesByDefFile = new Dictionary<DefXML, List<TargetNode>>();
        
        public Dictionary<string, Dictionary<string, List<TargetNode>>> GetTargetNodesSorted(FileInfo[] defFiles = null)
        {
            List<TargetNode> SelectedNodes = new List<TargetNode>();
            Dictionary<string, Dictionary<string, List<TargetNode>>> sorted = new Dictionary<string, Dictionary<string, List<TargetNode>>>();
            if (defFiles != null)
            {
                foreach (FileInfo defFile in defFiles)
                {
                    List<TargetNode> target = TargetNodesByDefFile.Where(item => item.Key.XMLFile.FullName == defFile.FullName).FirstOrDefault().Value;
                    if (target == null)
                        SimpleLog.WriteLine(string.Format("파일 {0} 에서 추출된 노드가 없습니다. ConfigData 에 따른 노드 추출물이 없거나, 파일 또는 ConfigData가 정상적이지 않습니다.", defFile.Name), ConsoleColor.DarkYellow);
                    else
                        SelectedNodes.AddRange(target);
                }
            }
            else
            {
                SimpleLog.WriteLine("파일을 선택하지 않았으므로, 추출된 모든 노드를 선택합니다...", ConsoleColor.DarkYellow);
                foreach(var item in TargetNodesByDefFile)
                    SelectedNodes.AddRange(item.Value);
            }

            foreach(TargetNode node in SelectedNodes)
            {
                string rootNodeName = node.RootDefNodeName;
                string defName = node.defName;
                if (string.IsNullOrEmpty(defName))
                {
                    SimpleLog.WriteLine(string.Format("타겟 노드 {0} 에는 defName 또는 def 이 없습니다. 노드를 건너뜁니다...", node.currentName), ConsoleColor.Red);
                    continue; //def 또는 defName이 없는 경우(정상적인 상태가 아님)
                }
                if (!sorted.ContainsKey(rootNodeName))
                    sorted.Add(rootNodeName, new Dictionary<string, List<TargetNode>>());
                if (!sorted[rootNodeName].ContainsKey(defName))
                    sorted[rootNodeName][defName] = new List<TargetNode>();
                sorted[rootNodeName][defName].Add(node);
            }

            return sorted;
        }

        public ConfigDataController(List<ConfigData> configDatas, FileInfo[] defFiles, DirectoryInfo exportdir)
        {
            this.ConfigDatas = configDatas;
            defFiles.ToList().ForEach(item => defXMLs.Add(DefXML.Load(item)));
            this.ExportDir = exportdir;
        }
        
        public void ParseDefFiles()
        {
            foreach (var configData in ConfigDatas)
            {
                foreach (var defXML in defXMLs)
                {
                    List<TargetNode> parsedNodes = defXML.TakeTargetNodes(configData);

                    if (parsedNodes.Count == 0)
                        continue;
                    if (!TargetNodesByDefFile.ContainsKey(defXML))
                    {
                        TargetNodesByDefFile.Add(defXML, new List<TargetNode>());
                    }
                    foreach(var item in parsedNodes)
                    {
                        // 여기서 노드 중복 검사를 하면 안됨 -> 어디서하냐~~~~
                        /*
                        bool isDuplicated = (from comparee in CollectedNodes
                                             where comparee.defName == item.defName && comparee.Value == item.Value && comparee.RootNodeName == item.RootNodeName
                                             select comparee).Count() > 0;
                        if (!isDuplicated)
                        {
                            SimpleLog.WriteLine("노드 추가 " + item.ToString(), ConsoleColor.Green);
                            TargetNodesByDefFile[defXML].Add(item);
                            CollectedNodes.Add(item);
                        }
                        else
                            SimpleLog.WriteLine("중복된 노드 스킵");
                            */
                        TargetNodesByDefFile[defXML].Add(item);
                    }
                }
            }
        }
    }
}
