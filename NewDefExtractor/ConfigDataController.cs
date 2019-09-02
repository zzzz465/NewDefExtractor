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
                string rootNodeName = node.RootNodeName;
                string defName = node.defName;
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
                        // 여기서 노드 중복 검사를 하면 안됨
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


        #region 노드 소팅 알고리즘
        static int CompareTargetNodeByRootName(TargetNode original, TargetNode target)
        {
            if (original == target)
                return 0;

            int result = string.Compare(original.RootNode.Name.LocalName, target.RootNode.Name.LocalName);
            return result;
        }


        static int CompareTargetNodesByDefName(TargetNode original, TargetNode target)
        {
            List<string> ElementOrder = new List<string>()
            {
                "label",
                "description"
            };

            int result = string.Compare(original.defName, target.defName);
            if (result != 0) // 만약 defName value가 다르다면.
                return result;
            //만약 두개가 같은 defName 안에 속해있는다면.
            string original_localName = original.currentName;
            string target_localName = target.currentName;
            if (original_localName == target_localName)
                return 0;

            int? original_index = ElementOrder.IndexOf(original_localName);
            int? target_index = ElementOrder.IndexOf(target_localName);
            if (original_index == null && target_index == null)
                return 0;
            else if (original_index != null && target_index == null)
                return -1;
            else if (original_index == null && target_index != null)
                return 1;
            else //둘다 아닌경우
            {
                if (original_index < target_index)
                    return -1;
                else if (original_index > target_index)
                    return 1;
                else
                    return 0;
            }
        }
        
        static int CompareTargetNodesByDepth(TargetNode original, TargetNode target)
        {
            int original_depth = original.AncestorsAndSelf.Count();
            int target_depth = target.AncestorsAndSelf.Count();
            if (original_depth < target_depth)
                return -1;
            else if (original_depth < target_depth)
                return 1;
            else
                return 0;
        }
        #endregion
    }
}
