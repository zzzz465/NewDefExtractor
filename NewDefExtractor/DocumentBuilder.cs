﻿using System;
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
            foreach (TargetNode node in unsortedNodes)
            {
                string NodeName = NodeBuilder(node);
                string defName = node.defName;
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
            string returnValue = string.Empty;
            List<string> values = new List<string>();
            ConfigData selector = node.NodeSelector;
            IEnumerator<XElement> AncestorsAndSelfEnumerator = node.AncestorsAndSelf.GetEnumerator();
            while (AncestorsAndSelfEnumerator.Current == null || AncestorsAndSelfEnumerator.Current.XPathSelectElement(selector.IgnoreBeforeThis) == null)
                AncestorsAndSelfEnumerator.MoveNext();

            do
            {
                XElement elem = AncestorsAndSelfEnumerator.Current;
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
            while (AncestorsAndSelfEnumerator.MoveNext());
            returnValue = string.Join(".", values.ToArray());
            return returnValue;
        }
    }
}
