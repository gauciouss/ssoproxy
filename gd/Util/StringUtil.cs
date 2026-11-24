using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gd.Util
{
    public class StringUtil
    {                
        /// <summary>
        /// 檢查字串是否為空白字串
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsNullOrWhiteSpace(string? str)
        {
            return string.IsNullOrWhiteSpace(str);
        }
    }
}