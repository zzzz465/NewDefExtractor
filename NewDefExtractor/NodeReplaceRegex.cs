using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewDefExtractor
{
    /// <summary>
    /// 하나의 NodeReplaceRegex 데이터를 나타냅니다
    /// </summary>
    public class NodeReplaceRegex
    {
        /// <summary>
        /// Xpath을 나타냅니다
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// 변경할 대상을 나타내는 Xpath, 또는 특수문자로 선언된 raw 문자열 또는 li 입니다.
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// Xpath인지 아닌지 나타내는 변수입니다.
        /// </summary>
        public readonly bool isXpath = true;

        public NodeReplaceRegex(bool isXpath, string key, string value)
        {
            this.isXpath = isXpath;
            this.Key = key;
            this.Value = value;
        }
    }
}
