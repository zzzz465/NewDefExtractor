using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Xml.XPath;
using System.Xml;
using System.Xml.Linq;

namespace NewDefExtractor
{
    /// <summary>
    /// Configs 하위의 노드 한개마다 생성되는 데이터
    /// </summary>
    public partial class ConfigData
    {
        /// <summary>
        /// 노드의 이름
        /// </summary>
        public string name { get; }
        /// <summary>
        /// 이 데이터의 Xpath
        /// </summary>
        public string Xpath { get; }
        /// <summary>
        /// NodeReplaceData 하위의 Regex - Xpath(또는 Raw value) 쌍을 담는 리스트.
        /// </summary>
        private List<NodeReplaceData> NodeReplaceDatas { get; } = new List<NodeReplaceData>();

        public IEnumerable<NodeReplaceData> GetNodeReplaceDatas { get { return NodeReplaceDatas.AsEnumerable(); } }
        /// <summary>
        /// 이 노드 이전까지 무시하도록 설정합니다. 내부 값은 Xpath가 와야합니다.
        /// </summary>
        public string IgnoreBeforeThis { get; set; }

        /// <summary>
        /// Config 인스턴스를 생성해서 반환
        /// </summary>
        /// <param name="name">해당 config의 이름</param>
        /// <param name="Config">해당 config의 jobj (Xpath, NodeReplaceData 등)</param>
        ConfigData(string name, JObject Config)
        {
            this.name = name;
            string Xpath = (Config["Xpath"] as JValue)?.Value as string;
            if (string.IsNullOrEmpty(Xpath))
                throw new ArgumentException(string.Format("config {0} 의 Xpath가 설정되어있지 않습니다.", name));

            //Xpath가 비정상이라면 예외를 throw 할 것
            XPathExpression.Compile(Xpath);

            this.Xpath = Xpath;

            JObject NodeReplaceRegex = (Config["NodeReplaceData"] as JObject); // 수정
            if (NodeReplaceRegex == null)
                throw new ArgumentException(string.Format("config {0}에 NodeReplaceRegex가 없거나, 정상적인 데이터가 아닙니다.", name));

            //IgnoreBeforeThis 태그가 있는지.
            JToken IgnoreBeforeThis_JToken = ((JValue)Config["IgnoreBeforeThisRegex"]);
            if(IgnoreBeforeThis_JToken != null)
            {
                if (((JValue)IgnoreBeforeThis_JToken).Value != null)
                    this.IgnoreBeforeThis = (IgnoreBeforeThis_JToken as JValue).Value.ToString();
            }

            if (string.IsNullOrEmpty(this.IgnoreBeforeThis))
                this.IgnoreBeforeThis = "./defName";

            ParseNodeReplaceRegex(NodeReplaceRegex);
        }

        /// <summary>
        /// <br>해당 노드를 가지고 가지고있는 NodeReplaceData와 비교합니다.</br>
        /// <br>찾는데 성공한다면 true, 아니면 false를 반환합니다.</br>
        /// </summary>
        /// <param name="targetElem"></param>
        /// <param name="nodeReplaceData"></param>
        /// <returns></returns>
        public bool FindMatchingConfigData(XElement targetElem, out NodeReplaceData nodeReplaceData)
        {
            string TargetNodeName = targetElem.Name.LocalName;
            foreach(NodeReplaceData data in this.GetNodeReplaceDatas)
            {
                if(data.replaceDataType == ReplaceDataType.Regex)
                {
                    if (Regex.IsMatch(TargetNodeName, data.Key))
                    {
                        nodeReplaceData = data;
                        return true;
                    }
                }
                else if(data.replaceDataType == ReplaceDataType.Xpath)
                {
                    if(targetElem.XPathSelectElement(data.Key) != null)
                    {
                        nodeReplaceData = data;
                        return true;
                    }
                }
            }
            nodeReplaceData = null;
            return false;
        }

        void ParseNodeReplaceRegex(JObject NodeReplaceRegex_)
        {
            //여기서 딕셔너리 형태가 어떻게 되는지 알아야함.
            foreach(KeyValuePair<string, JToken> item in NodeReplaceRegex_)
            {
                /*
                 * 문자열이 #로 시작한다면 li와 관련, $로 시작하면 그냥 문자열 자체로 교체하겠다는 뜻. 을 인식하도록 설정
                 NodeReplaceData 인스턴스를 생성, key value 값을 넣어줌.
                 그리고 NodeReplaceData 리스트에 추가
                 */
                string key = item.Key;
                string value = (item.Value as JValue).Value as string;
                ReplaceDataType dataType = ReplaceDataType.Regex; // 기본값
                if (value.StartsWith("%"))
                    dataType = ReplaceDataType.Xpath;
                //else
                //    dataType = ReplaceDataType.Regex;
                NodeReplaceData regexData = new NodeReplaceData(dataType, key, value);
                NodeReplaceDatas.Add(regexData);
            }
        }
    }

    public partial class ConfigData
    {
        /// <summary>
        /// Config file과 그 안의 ConfigData 에 대한 리스트
        /// </summary>
        static Dictionary<FileInfo, List<ConfigData>> Configs { get; } = new Dictionary<FileInfo, List<ConfigData>>();

        public static List<ConfigData> Load(FileInfo configFile)
        {
            return Load(File.ReadAllText(configFile.FullName));
        }

        public static List<ConfigData> Load(string jsonData)
        {
            JObject ConfigDataRoot = JObject.Parse(jsonData);
            if (ConfigDataRoot == null)
                throw new ArgumentException("컨픽 은 정상적인 데이터 형식을 갖추지 않았습니다.");

            //other validation 을 마치고 괜찮다면.

            List<ConfigData> tempConfigList = new List<ConfigData>(); // 여기다가 하위의 ConfigData를 저장

            JObject Configs = ConfigDataRoot["Configs"] as JObject;
            foreach (dynamic item in Configs)
            {
                try
                {
                    string name = item.Key;
                    SimpleLog.WriteLine(string.Format("config {0}을 패턴에 추가합니다...", name));
                    ConfigData data = new ConfigData(name, item.Value as JObject);
                    tempConfigList.Add(data);
                }
                catch(XPathException ex)
                {
                    SimpleLog.WriteLine("Config {0} 의 Xpath value가 정상적이지 않습니다. 이 데이터를 건너뜁니다...", ConsoleColor.Red);
                }
            }
            //throw new NotImplementedException();
            return tempConfigList;
        }
    }
}
