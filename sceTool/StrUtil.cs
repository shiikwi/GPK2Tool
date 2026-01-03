using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace sceTool
{
    public static class StrUtil
    {
        public static List<string> SplitByNull(byte[] data)
        {
            var list = new List<string>();
            int start = 0;
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == 0)
                {
                    if (i > start)
                        list.Add(Encoding.GetEncoding("shift_jis").GetString(data, start, i - start));
                    start = i + 1;
                }
            }
            if (list.Count < 2) list.Add(string.Empty);
            return list;
        }

        public static uint? GetSymbolOffset(string str)
        {
            if(!string.IsNullOrEmpty(str) && str.Contains(".scb:"))
            {
                var match = Regex.Match(str, @"^(.*\.scb):(\d+)$");
                if (match.Success)
                    return uint.Parse(match.Groups[2].Value);
            }
            return null;
        }

    }
}
