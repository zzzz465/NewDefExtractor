﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace NewDefExtractor
{
    public class TargetNode : IComparable<TargetNode>
    {
        /// <summary>
        /// 바로 상위 노드를 참조합니다
        /// </summary>
        public XElement ParentNode { get { return CurrentNode.Parent; } }
        /// <summary>
        /// 이 노드를 선택한 ConfigData를 반환합니다
        /// </summary>
        public ConfigData NodeSelector { get; }

        readonly XElement CurrentNode;
        public string defName
        {
            get
            { //null 일 경우가 있을 수 있음.
                return AncestorsAndSelf.FirstOrDefault().XPathSelectElement(".//defName").Value;
            }
        }

        public string currentName
        {
            get
            {
                return CurrentNode.Name.LocalName;
            }
        }

        public string currentNodeValue
        {
            get
            {
                return CurrentNode.Value;
            }
        }

        /// <summary>
        /// 최상위 노드(Defs 바로 아래)를 참조합니다
        /// </summary>
        public XElement RootNode
        {
            get
            {
                return AncestorsAndSelf.FirstOrDefault();
            }
        }

        public string RootNodeName
        {
            get
            {
                return RootNode.Name.LocalName;
            }
        }
        public string Value { get; }
        /// <summary>
        /// Defs 노드를 제외한 모든 노드를 반환합니다.
        /// </summary>
        public IEnumerable<XElement> AncestorsAndSelf
        {
            get
            {
                return CurrentNode.AncestorsAndSelf().Where(node => node.Name != "Defs").Reverse();
            }
        }

        public TargetNode(XElement targetElement, ConfigData configData)
        {
            if (string.IsNullOrEmpty(targetElement.Value))
                throw new Exception("노드 " + targetElement.Name + " 의 value 가 null 또는 empty 입니다");

            this.Value = targetElement.Value;
            this.CurrentNode = targetElement;
            this.NodeSelector = configData;

            if (this.AncestorsAndSelf.FirstOrDefault().Attribute("Abstract")?.Value.ToLower() == "true") // Abstract 은 제외
                throw new Exception("Abstract def는 추가 대상이 아닙니다");
        }

        public override string ToString()
        {
            return string.Format("node {0} | value {1}", CurrentNode.Name.LocalName, CurrentNode.Value);
        }

        public int CompareTo(TargetNode other)
        {
            //defname으로 정렬
            int CompareDefName = string.Compare(this.defName, other.defName);
            if (CompareDefName != 0)
                return CompareDefName;

            int i = 0;
            int this_nodeLength = this.AncestorsAndSelf.Count();
            int other_nodeLength = other.AncestorsAndSelf.Count();
            /*
            if (this_nodeLength < other_nodeLength)
                return -1;
            else if (this_nodeLength > other_nodeLength)
                return 1;
            else*/
            {
                int least_max = this.AncestorsAndSelf.Count() > other.AncestorsAndSelf.Count() ? other.AncestorsAndSelf.Count() : this.AncestorsAndSelf.Count(); // 항상 동일하므로 별 의미는 없음
                for (i = 0; i < least_max; i++)
                {
                    string this_localname = this.AncestorsAndSelf.ToList()[i].Name.LocalName;
                    string other_localname = other.AncestorsAndSelf.ToList()[i].Name.LocalName;
                    int CompareResult = string.Compare(this_localname, other_localname);
                    return CompareResult;
                }
            }

            //마지막 노드 이름으로 정렬
            List<string> ElementOrder = new List<string>()
            {
                "label",
                "description"
            };

            int result = string.Compare(this.defName, other.defName);
            if (result != 0) // 만약 defName value가 다르다면.
                return result;
            //만약 두개가 같은 defName 안에 속해있는다면.
            string original_localName = this.currentName;
            string target_localName = other.currentName;
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
            //int CompareLastLabelName 
        }
    }
}
