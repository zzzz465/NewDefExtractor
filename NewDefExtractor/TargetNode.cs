using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;

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
        {//만약 Patch에 대한걸 고치려면, xpath과 관련해서 노드명을 직접 적어줘야함... -> 여기로 옮기자.? X
            get
            { //null 일 경우가 있을 수 있음. -> PatchOperation일때
                foreach(XElement elem in AncestorsAndSelf.Reverse())
                {
                    if(elem.XPathSelectElement("./defName") != null)
                    {
                        return elem.XPathSelectElement("./defName").Value;
                    }
                    else if(elem.XPathSelectElement("./def") != null)
                    {
                        return elem.XPathSelectElement("./def").Value;
                    }
                }
                return null;
                //    AncestorsAndSelf.FirstOrDefault().XPathSelectElement(".//defName").Value;
                //if (!string.IsNullOrEmpty(returnValue))
                //    return returnValue;
                //return AncestorsAndSelf.FirstOrDefault().XPathSelectElement(".//def").Value;
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
        /*
        /// <summary>
        /// 최상위 노드(Defs 바로 아래)를 참조합니다
        /// </summary>
        public XElement RootDefNode
        {
            get
            {
                if(AncestorsAndSelf.First().Name.LocalName == "Patch") // patch 에 대해서
                {
                    foreach(var item in AncestorsAndSelf.Reverse())
                    {
                        if(item.Element("xpath") != null)
                        {
                            return item.Element("xpath");
                        }
                    }
                }
                else
                {
                    foreach(var item in AncestorsAndSelf.Reverse())
                    {
                        if(item.Element("defName") != null)
                        {
                            return item.Element("defName");
                        }
                    }
                }
                return;
            }
        }*/

        public XElement RawXpath
        {
            get
            {
                XElement li_PatchRootNode = this.AncestorsAndSelf.Reverse() // li Class=PatchOperation어쩌고 노드를 선택
                                                .Where(node => (node.Attribute("Class")?.Value.Contains("PatchOperation") == true))
                                                .FirstOrDefault();
                return null;
            }
        }

        public string RootDefNodeName
        {
            get
            {
                if(this.isPatch)
                {
                    if(RawXpath != null)
                    {
                        string rawXpath = RawXpath.Value;
                        Match result = Regex.Match(rawXpath, "(?<=defName=\")[\\w] + (?= \"])");
                        if (result.Success)
                        {
                            return result.Value;
                        }
                        else
                            throw new Exception("Patch xpath를 파싱하는 도중 defName을 찾을 수 없습니다..");
                    }
                    return null;
                    /*
                    foreach(var item in this.AncestorsAndSelf.Reverse())
                    {
                        string rawXpath = item.Element("xpath")?.Value;
                        if (!string.IsNullOrEmpty(rawXpath))
                        {
                            Match result = Regex.Match(rawXpath, "(?<=defName=\")[\\w] + (?= \"])");
                            if (result.Success)
                            {
                                return result.Value;
                            }
                            else
                                throw new Exception("Patch xpath를 파싱하는 도중 defName을 찾을 수 없습니다..");
                        }
                    }
                    return null;
                    */
                }
                else
                {//Defs를 제외한 노드들이 AncestorsAndSelf 에 오므로, 첫번째 노드인 XXXDef 관련 데이터가 오게 될 것.
                    return this.AncestorsAndSelf.FirstOrDefault()?.Name.LocalName;
                }
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
                return CurrentNode.AncestorsAndSelf().Where(
                    node => node.Name != "Defs" 
                    || node.Name != "Patch" 
                    || node.Name != "match" 
                    || node.Name != "operations").Reverse();
            }
        }

        public bool isPatch
        {
            get
            {
                if (CurrentNode.AncestorsAndSelf().First().Name.LocalName == "Defs")
                    return false;
                else
                    return true;
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

            //if (this.defName == "CreateMedicalRib" && other.defName == "CreateMedicalRib")
            //    Debugger.Break();

            //길이로 비교

            int depth1 = this.AncestorsAndSelf.Count();
            int depth2 = other.AncestorsAndSelf.Count();

            if (depth1 < depth2)
                return -1;
            else if (depth1 > depth2)
                return 1;

            XElement[] nodecollection1 = this.AncestorsAndSelf.ToArray();
            XElement[] nodecollection2 = other.AncestorsAndSelf.ToArray();

            for (int i = 0; i < this.AncestorsAndSelf.Count(); i++)
            {
                string nodename1 = nodecollection1[i].Name.LocalName;
                string nodename2 = nodecollection2[i].Name.LocalName;
                int compareResult = GetOrder(nodename1, nodename2);
                if (compareResult != 0)
                    return compareResult;
                else
                    continue;
            }

            return 0;
            /*
            int i = 0;
            int this_nodeLength = this.AncestorsAndSelf.Count();
            int other_nodeLength = other.AncestorsAndSelf.Count();
            /*
            if (this_nodeLength < other_nodeLength)
                return -1;
            else if (this_nodeLength > other_nodeLength)
                return 1;
            else
            {
                int least_max = this.AncestorsAndSelf.Count() > other.AncestorsAndSelf.Count() ? other.AncestorsAndSelf.Count() : this.AncestorsAndSelf.Count(); // 항상 동일하므로 별 의미는 없음
                for (i = 0; i < least_max; i++)
                {
                    string this_localname = this.AncestorsAndSelf.ToList()[i].Name.LocalName;
                    string other_localname = other.AncestorsAndSelf.ToList()[i].Name.LocalName;
                    int CompareResult = string.Compare(this_localname, other_localname);
                    return CompareResult;
                }
            }*/

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

        int GetOrder(string value1, string value2)
        {
            //int num1 = -1;
            //int num2 = -1;
            //bool flag1 = int.TryParse(value1, out num1);
            //bool flag2 = int.TryParse(value2, out num2);
            //
            //if(flag1 && flag2)
            //{
            //    if (num1 < num2)
            //        return -1;
            //    if (num1 > num2)
            //        return 1;
            //}
            //
            List<string> fixedOrder = new List<string>()
            {
                "label",
                "description"
            };
            int index1 = fixedOrder.IndexOf(value1);
            int index2 = fixedOrder.IndexOf(value2);

            if (index1 == -1 && index2 == -1)
                return string.Compare(value1, value2);
            else if (index1 == -1 && index2 != -1)
                return 1;
            else if (index1 != -1 && index2 == -1)
                return -1;
            else //둘다 안에 있을경우
            {
                if (index1 < index2)
                    return -1;
                if (index1 > index2)
                    return 1;
                return 0;
            }
        }
    }
}
