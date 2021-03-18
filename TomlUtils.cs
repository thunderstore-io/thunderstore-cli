using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Tommy;

namespace ThunderstoreCLI
{
    public static class TomlUtils
    {
        public static TomlTable DictToToml(Dictionary<string, string> dict)
        {
            var result = new TomlTable();
            foreach (var kvp in dict)
            {
                result.Add(kvp.Key, kvp.Value);
            }
            return result;
        }

        public static string FormatToml(TomlTable toml)
        {
            using (var writer = new StringWriter())
            {
                writer.NewLine = "\n";
                toml.WriteTo(writer);
                writer.Flush();
                return writer.ToString().Trim();
            }
        }
    }
}
