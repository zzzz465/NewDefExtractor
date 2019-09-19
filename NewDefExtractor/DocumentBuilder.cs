using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections;
using System.Xml.XPath;

namespace NewDefExtractor
{
    public static class DocumentBuilder
    {
        static Regex liRegex = new Regex("li\\[[\\d]+\\]");
        static Regex intRegex = new Regex("(?<=\\[)[\\d]+(?=\\])");
        public static bool ShowOriginalString { get; set; } = true;
        /// <summary>
        /// 주어진 노드를 가지고 하나의 XDoc를 생성함
        /// </summary>
        /// <param name="unsortedNodes">Xdoc 내부의 컨텐츠</param>
        /// <returns></returns>
        public static XDocument PrepareXDoc(List<TargetNode> unsortedNodes)
        {//indent는 xml 작성할떄 해야함.
            //여기도 수정했음
            //unsortedNodes.Sort();
            XDocument doc = new XDocument();
            doc.Add(new XElement("LanguagesData"));
            XElement root = doc.Root;
            List<XNode> NodesToAdd = new List<XNode>();
            string latestDefname = string.Empty;

            List<string> AddedNodes = new List<string>(); // 노드 중복을 제거하기 위해서 여기다 저장 및 체크

            foreach (TargetNode node in unsortedNodes)
            {
                string NodeName = NodeBuilder(node);
                string defName = node.defName;

                if (AddedNodes.Contains(NodeName))
                    continue;
                else
                    AddedNodes.Add(NodeName);

                if (defName != latestDefname)
                {
                    latestDefname = defName;
                    NodesToAdd.Add(new XComment(latestDefname));
                    NodesToAdd.Add(new XComment(string.Empty));
                    //NodesToAdd.Add(new x)
                }
                if(ShowOriginalString)
                    NodesToAdd.Add(new XComment(string.Format("Original << {0}", node.Value)));
                NodesToAdd.Add(new XElement(NodeName, node.Value));
            }
            doc.Root.Add(NodesToAdd);
            return doc;
        }

        //public static void WriteXML()

        static string NodeBuilder(TargetNode node)
        {
            string returnValue = string.Empty;
            if(node.isPatch)
            {
                XElement valueNode = node.ValueNode;
                if(valueNode == null)
                {
                    SimpleLog.WriteLine(string.Format("노드 {0}는 패치 노드이지만 Value 태그가 없습니다. 잘못된 노드입니다...", node), ConsoleColor.Red);
                    return string.Empty;
                }
                string POClass = valueNode.Parent.Attribute("Class")?.Value;
                if(POClass == "PatchOperationAdd")
                {
                    returnValue = POAddNodeStringBuilder(node);
                }
                else if(POClass == "PatchOperationReplace")
                {
                    returnValue = POReplaceNodeStringBuilder(node);
                }
                else
                {
                    throw new Exception("PatchOperationAdd / PatchOperationReplace 이외의 메소드는 지원하지 않습니다");
                }
            }
            else
            {
                List<XElement> nodesToAdd = new List<XElement>();
                ConfigData selector = node.NodeSelector;
                bool flag = true; // X 이전 건너뛰기 위한 분기점

                foreach(XElement elem in node.AncestorsAndSelf)
                {
                    if(flag && elem.Parent?.Element("defName") == null)
                        continue;
                    else
                        flag = false;
                    nodesToAdd.Add(elem);
                }
                returnValue = string.Join(".", ChangeNodeValues(nodesToAdd, node));
            }
            return string.Format("{0}.{1}", node.defName, returnValue);
        }

        static string POAddNodeStringBuilder(TargetNode targetNode)
        {
            XElement ValueNode = targetNode.ValueNode;
            string rawXpath = ValueNode.XPathSelectElement("../xpath").Value;
            string Xpath = Regex.Match(rawXpath, "(?<=defName=\"[\\w]+\"])(/[\\w]+(\\[[\\d]+\\])*)+").Value.Substring(1);
            string defName = targetNode.defName;
            
            bool isAddingNewDef = Regex.Match(rawXpath, "(?<=defName=\")[\\w]+(?=\")") == null ? true : false;


            IEnumerator<XElement> enumerator = targetNode.AncestorsAndSelf.GetEnumerator();
            while(enumerator.MoveNext())
            {
                if(isAddingNewDef)
                {
                    if(enumerator.Current.XPathSelectElement("./defName") != null) // XXXDef 에서 break
                        break;
                }
                else
                {
                    if(enumerator.Current.Name.LocalName == "value") // Value node 에서 break
                        break;
                }
            }
            List<XElement> NodesToAdd = new List<XElement>();
            while(enumerator.MoveNext())
                NodesToAdd.Add(enumerator.Current);

            return string.Join(".", ChangeNodeValues(NodesToAdd, targetNode));
        }

        static string POReplaceNodeStringBuilder(TargetNode targetNode)
        {
            XElement ValueNode = targetNode.ValueNode;
            string rawXpath = ValueNode.XPathSelectElement("../xpath").Value;
            string Xpath = Regex.Match(rawXpath, "(?<=defName=\"[\\w]+\"])(/[\\w]+(\\[[\\d]+\\])*)+").Value.Substring(1);
            string defName = targetNode.defName;
            List<string> nodesToAdd = Xpath.Split('/').ToList();
            string last = nodesToAdd.Last();
            int i = -1;
            List<string> nodesToAddModified = new List<string>();
            nodesToAdd.ForEach(item =>
            {
                i++;
                if (liRegex.Match(item).Success)
                    nodesToAddModified.Add(intRegex.Match(item).Value);
                else
                    nodesToAddModified.Add(item);
            });
            int result = 0;
            if(int.TryParse(intRegex.Match(last)?.Value, out result) && targetNode.currentName == "li")
                nodesToAddModified[i] = result.ToString();

            return string.Join(".", nodesToAddModified);
        }

        static List<string> ChangeNodeValues(List<XElement> originalNodes, TargetNode node)
        {
            List<string> nodesToAdd = new List<string>();
            foreach (XElement elem in originalNodes)
            {
                if (originalNodes.IndexOf(elem) == 0 && node.isPatch)
                {
                    nodesToAdd.Add(elem.Name.LocalName);
                    continue;
                }
                NodeReplaceData repData;
                if (node.NodeSelector.FindMatchingConfigData(elem, out repData))
                {//만약 NodeReplaceData가 있다면 -> 값을 바꿔야 한다는 뜻
                 //만약 상위 노드가 value 라면, 첫 노드를 추가하는 것이므로... 노드 이름을 숫자로 치환하면 안됨
                    if (repData.Value.StartsWith("#Count")) // 만약 #Count 라면 li 숫자로 바꿔줘야함
                    {
                        int index = node.CurrentNode.ElementsBeforeSelf().Count();
                        nodesToAdd.Add(index.ToString());
                    }
                    else
                    {// 아니라면 xpath에 기반한 값으로 바꿔줘야함
                        string value = node.CurrentNode.XPathSelectElement(repData.Value)?.Value;
                        if (string.IsNullOrEmpty(value)) // 만약 없다면 에러 메세지 출력
                            SimpleLog.WriteLine(string.Format("Xelement {0} 의 노드를 작성하던 도중 비어있는 노드를 발견했습니다.\n 해당 노드는 {1} 입니다.",
                            node,
                            string.Join(".", elem.AncestorsAndSelf().Select(item => item.Name.LocalName))), ConsoleColor.Red);
                        else
                            nodesToAdd.Add(value);
                    }
                }
                else
                {//NodeRepData 에 안걸린다면 그냥 쌩 데이터 추가
                    nodesToAdd.Add(elem.Name.LocalName);
                    continue;
                }
            }
            return nodesToAdd;
        }
    }
}
