using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml;

namespace NewDefExtractor
{
    /// <summary>
    /// Def 폴더 내부의 XML 한개에 대한 인스턴스
    /// </summary>
    public class DefXML
    {
        /// <summary>
        /// 중복된 노드를 제거하기 위해, 파싱된 노드들을 저장하는 변수입니다.
        /// </summary>
        public static List<TargetNode> CollectedNodes = new List<TargetNode>();
        public FileInfo XMLFile { get; private set; }
        XDocument XDoc { get; }

        //public IEnumerable<TargetNode> GetParsedElements { get { return ParsedElements.AsEnumerable(); } }
        
        //XDocument 
        private DefXML(FileInfo xmlFile)
        {
            XDoc = XDocument.Parse(File.ReadAllText(xmlFile.FullName));
        }

        /// <summary>
        /// ConfigData를 기반으로 XML을 파싱합니다.
        /// </summary>
        /// <param name="ConfigData">파싱 조건인 ConfigData입니다. 없을경우 인스턴스 생성 당시의 설정 Config를 이용</param>
        public List<TargetNode> TakeTargetNodes(ConfigData ConfigData)
        {
            List<TargetNode> ParsedElements = new List<TargetNode>();
            foreach(XElement targetXElement in XDoc.XPathSelectElements(ConfigData.Xpath))
            {//Xpath에 해당하는 element를 반환
                try
                {
                    TargetNode node = new TargetNode(targetXElement, ConfigData);
                    //여기 중복 체크가 제대로 되는지 확인하자. //FIXME
                    /* 기존 알고리즘 (부모 노드도 동일한지 체크)
                    bool isDuplicated = (from CollectedNode in CollectedNodes
                                        where CollectedNode.defName == node.defName && CollectedNode.ParentNode == node.ParentNode && node.currentNodeValue == CollectedNode.Value
                                        select CollectedNode).FirstOrDefault() != null;
                                         */
                     
                    bool isDuplicated = (from CollectedNode in CollectedNodes
                                         where CollectedNode.defName == node.defName && node.currentNodeValue == CollectedNode.Value
                                         select CollectedNode).FirstOrDefault() != null;
                    if (!isDuplicated)
                    {
                        ParsedElements.Add(node);
                        CollectedNodes.Add(node);
                        SimpleLog.WriteLine(string.Format("노드 {0} 을 리스트에 추가합니다...", node.ToString()), ConsoleColor.Green);
                    }
                    else
                        SimpleLog.WriteLine(string.Format("중복된 노드 {0} 을 건너뜁니다..", targetXElement.ToString()), ConsoleColor.DarkMagenta);
                }
                catch
                {
                    SimpleLog.WriteLine(string.Format("에러가 발생한 노드 {0} 을 건너뜁니다..", targetXElement.ToString()), ConsoleColor.DarkMagenta);
                }
            }
            return ParsedElements;
        }

        /// <summary>
        /// <br>Load XML and create an instance about it.</br>
        /// <br>throw ArgumentException when fail to read xml</br>
        /// </summary>
        /// <param name="file">tar</param>
        /// <returns></returns>
        public static DefXML Load(FileInfo file)
        {
            if (false)
                throw new ArgumentException("file " + file.Name + " is not valid XML file");
            //validation 이 끝나면,
            DefXML instance = new DefXML(file) { XMLFile = file };
            return instance;
        }
    }
}
