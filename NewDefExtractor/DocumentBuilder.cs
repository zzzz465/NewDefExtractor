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
                    //NodesToAdd.Add(new x)
                }
                NodesToAdd.Add(new XElement(NodeName, node.Value));
            }
            doc.Root.Add(NodesToAdd);
            return doc;
        }

        //public static void WriteXML()

        static string NodeBuilder(TargetNode node)
        {
			List<XElement> nodes = new List<XElement>();
			List<string> nodes2 = new List<string>();
            if (node.isPatch) // 패치 타입을 가져올 경우 좀 더 단순함
            {
                if(node.AncestorsAndSelf.Reverse().Where(item => item.XPathSelectElement("././defName") != null).FirstOrDefault() != null)
				{//defName 을 하위로 갖고있는 XXXDef 임
					IEnumerator<XElement> enumerator = node.AncestorsAndSelf.GetEnumerator();
					while(enumerator.MoveNext()) // need code fix.
					{
						if (enumerator.Current.Element("defName") != null)
						{
							do
							{
								nodes.Add(enumerator.Current);
							}
							while (enumerator.MoveNext());
						}
					}
				}
				else
				{
					XElement Origin_li_node = node.AncestorsAndSelf.Reverse()
																.Where(item => item.XPathSelectElement("../../xpath") != null)
																.Select(item => item)
																.FirstOrDefault();

					string rawXpath = Origin_li_node.XPathSelectElement("../../xpath").Value;
					string Xpath = Regex.Match(rawXpath, "(?<=defName=\"[\\w]*\"]/)[\\w/]+").Value;
					MatchCollection collection = Regex.Matches(Xpath, "\\[[\\d]+\\]");
					foreach(Match matched in collection)
					{
						string num = Regex.Match(matched.Value, "[\\d]+").Value;
						Xpath.Replace(matched.Value, num);
					}
					nodes2.AddRange(Xpath.Split('/'));
					nodes2.Add(Origin_li_node.Element("def")?.Value);
					
					IEnumerator<XElement> enumerator = node.AncestorsAndSelf.GetEnumerator();
					while(enumerator.MoveNext())
					{
						if (enumerator.Current.XPathSelectElement("./def") != null)
							break;
					}
					while (enumerator.MoveNext())
					{
						nodes.Add(enumerator.Current);
					}
				}
            }
			else
				nodes.AddRange(node.AncestorsAndSelf);


            string returnValue = string.Empty;
            List<string> values = new List<string>();
            ConfigData selector = node.NodeSelector;
            bool flag = true; // X 이전 건너뛰기 위한 분기점

            foreach(XElement elem in nodes)
            {
				if(!node.isPatch)
				{
                if (elem.XPathSelectElement(selector.IgnoreBeforeThis) != null)
                    flag = false;
                if (flag)
                    continue;
				}

                //string ValueToReplace = string.Empty;
                NodeReplaceData repData;
                if(!selector.FindMatchingConfigData(elem, out repData))
                {
                    values.Add(elem.Name.LocalName);
                    continue;
                }

                string Xpath = repData.Value;

                if (Xpath.Equals("#Count")) // li 전용
                {
                    values.Add(elem.ElementsBeforeSelf().Count().ToString());
                }
                else if (Xpath.StartsWith("$"))
                {
                    values.Add(Xpath.Substring(1));
                }
                else if (!string.IsNullOrEmpty(Xpath)) // 해당하는게 있다면
                {
                    string ValueToReplace = elem.XPathSelectElement(Xpath)?.Value;
                    if (string.IsNullOrEmpty(ValueToReplace))
                    {
                        SimpleLog.WriteLine(string.Format("{0} 의 설정값인 Xpath {1} 의 값을 찾을 수 없습니다.", node.ToString(), Xpath), ConsoleColor.DarkYellow);
                        continue;
                    }
                    values.Add(ValueToReplace);
                }
                else
                    values.Add(elem.Name.LocalName);
            }

            returnValue = string.Join(".", values.ToArray());
			if(nodes2.Count > 0)
			{
				returnValue = node.defName + "." + string.Join(".", nodes2.ToArray()) + "." + returnValue;
			}
            return returnValue;
        }
    }
}
