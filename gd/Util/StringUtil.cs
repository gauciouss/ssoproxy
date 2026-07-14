using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gd.Util
{
    public class StringUtil
    {
        public static bool IsNullOrEmpty(string? str)
        {
            return string.IsNullOrEmpty(str);
        }

        public static string? Trim(string? str)
        {
            return str?.Trim();
        }

        public static bool IsNullOrWhiteSpace(string? str)
        {
            return string.IsNullOrWhiteSpace(str);
        }
    }
}