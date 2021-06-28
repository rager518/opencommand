using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCommand.Utils
{
    public static class StringExtension
    {
        public static bool IsNullOrWhiteSpace(this string str)
        {
            if (string.IsNullOrEmpty(str)) return true;
            return str.Trim().Length == 0;
        }
    }
}
