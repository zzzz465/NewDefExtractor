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
            //PatchOperationAdd 일때, PatchOperationReplace 일때, 아닐때 전부 구별해서 메소드를 만들자
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
					XElement ValueNode = node.AncestorsAndSelf.Reverse()
																.Where(item => item.XPathSelectElement("../xpath") != null)
																.Select(item => item)
																.FirstOrDefault();

					string rawXpath = ValueNode.XPathSelectElement("../xpath").Value;
                    //string Xpath = Regex.Match(rawXpath, "(?<=defName=\"[\\w]*\"]/)[\\w/]+").Value;
                    string Xpath = Regex.Match(rawXpath, "(?<=defName=\"[\\w]+\"])(/[\\w]+(\\[[\\d]+\\])*)+").Value.Substring(1);
                    string defName = Regex.Match(rawXpath, "(?<=defName=\")[\\w]+(?=\")").Value;
                    string ClassAttribute = ValueNode.XPathSelectElement("../.").Attribute("Class")?.Value;

                    // li[숫자] 를 숫자 로 바꿔줌
					MatchCollection collection = Regex.Matches(Xpath, "li\\[[\\d]+\\]");
					foreach(Match matched in collection)
					{
						string num = Regex.Match(matched.Value, "[\\d]+").Value;
						Xpath = Xpath.Replace(matched.Value, num);
					}

                    //PatchOperationAdd 일때
                    //PatchOperationAdd 이거 제대로 수정을 해줘야 함.
                    if (ClassAttribute == "PatchOperationAdd" && ValueNode.Element("def") != null) 
                    {
                        nodes2.AddRange(Xpath.Split('/'));
                        nodes2.Add(ValueNode.Element("def")?.Value);

                        IEnumerator<XElement> enumerator = node.AncestorsAndSelf.GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            if (enumerator.Current.XPathSelectElement("./def") != null)
                                break;
                        }
                        while (enumerator.MoveNext())
                        {
                            nodes.Add(enumerator.Current);
                        }
                    }

                    //PatchOperationReplace일때, 값을 변경하는 것이므로(노드를 삭제하고 새 노드를 집어넣음) 그냥 바로 빌드된 노드를 반환
                    else
                    {
                        List<string> nodesToAdd = Xpath.Split('/').ToList();
                        string last = nodesToAdd.Last();
                        nodesToAdd = nodesToAdd.Take(nodesToAdd.Count - 1).ToList();
                        string returnValue2 = defName;
                        //if (nodesToAdd.Count > 0)
                        //    returnValue2 += "." + string.Join(".", nodesToAdd);
                        IEnumerator<XElement> enumerator = node.AncestorsAndSelf.GetEnumerator();
                        while(enumerator.MoveNext())
                        {
                            if (enumerator.Current.Name.LocalName == "value")
                                break;
                        }
                        while(enumerator.MoveNext())
                        {
                            nodesToAdd.Add(enumerator.Current.Name.LocalName);
                        }
                        if (node.currentName == "li" && int.TryParse(last, out _))
                            nodesToAdd[nodesToAdd.Count - 1] = last;
                        return defName + "." + string.Join(".", nodesToAdd);
                        /*
                        returnValue2 += ".";
                        returnValue2 += (node.currentName == "li") ? last : node.currentName;
                        */


                        //string.Join(".", new string[] { defName, string.Join(".", nodesToAdd.Take(nodesToAdd.Length - 1)), node.currentName });
                        //returnValue2 = returnValue2 + Regex.Match()
                        //nodes2.AddRange(nodesToAdd.Take(nodesToAdd.Length - 1));
                        //nodes.Add(node.CurrentNode);
                        return returnValue2;
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
