using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace NewDefExtractor
{
    public static class DefXMLLoader
    {
        /// <summary>
        /// Def 하위의 모든 XML을 읽어옵니다
        /// </summary>
        /// <param name="DefsFolder">Defs 폴더에 대한 DirectoryInfo instance</param>
        public static FileInfo[] LoadXmls(DirectoryInfo DefsFolder)
        {
            FileInfo[] XMLs = DefsFolder.GetFiles("*.xml", SearchOption.AllDirectories);
            return XMLs;
        }
    }
}
