using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewDefExtractor
{
    /// <summary>
    /// 하나의 NodeReplaceData 데이터를 나타냅니다
    /// </summary>
    public class NodeReplaceData
    {
        /// <summary>
        /// 원본 문자열을 나타냅니다. %Xpath, 정규식 이 올 수 있습니다.
        /// </summary>
        private string RawKeyValue;
        /// <summary>
        /// 대상을 선택하는 문자열입니다. (%)Xpath, Regex, #Count 또는 ($문자열 -> 그대로 대입) 이 올 수 있습니다.
        /// </summary>
        public string Key
        {
            get
            {
                if (this.replaceDataType != ReplaceDataType.Regex)
                    return RawKeyValue.Substring(1);
                else
                    return RawKeyValue;
            }
        }
        /// <summary>
        /// 변경할 대상을 나타내는 Xpath, 또는 $문자열 또는 #Count 입니다.
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// Xpath인지 아닌지 나타내는 변수입니다.
        /// </summary>
        public readonly ReplaceDataType replaceDataType;
        /// <summary>
        /// defName 이전 노드들은 추가하지 않는 등의 기존 설정을 무시하는지
        /// </summary>
        [Obsolete]
        public bool IgnoreDeafultSetting { get; private set; }

        public NodeReplaceData(ReplaceDataType DataType, string key, string value)
        {
            this.replaceDataType = DataType;
            this.RawKeyValue = key;
            this.Value = value;
        }
    }

    public enum ReplaceDataType
    {
        Xpath = 2, // 현재 노드에서 Key Xpath에 대한 validation을 체크하여 그 노드의 Value를 선택
        Regex = 4 // 정규식을 통해서 노드를 매칭시킬 때
    }
}
