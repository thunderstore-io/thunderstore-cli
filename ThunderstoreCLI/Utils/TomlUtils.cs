using ThunderstoreCLI.Config;
using Tommy;

namespace ThunderstoreCLI
{
    public static class TomlUtils
    {
        public static TomlTable DictToTomlTable(Dictionary<string, string> dict)
        {
            var result = new TomlTable();
            foreach (KeyValuePair<string, string> kvp in dict)
            {
                result.Add(kvp.Key, kvp.Value);
            }
            return result;
        }

        public static TomlArray BuildCopyPathTable(List<CopyPathMap> list)
        {
            var result = new TomlArray() { IsTableArray = true };
            foreach (CopyPathMap entry in list)
            {
                result.Add(DictToTomlTable(new()
                {
                    { "source", entry.From },
                    { "target", entry.To }
                }));
            }
            return result;
        }

        public static string FormatToml(TomlTable toml)
        {
            using var writer = new StringWriter();
            writer.NewLine = "\n";
            toml.WriteTo(writer);
            writer.Flush();
            return writer.ToString().Trim();
        }

        public static string? SafegetString(TomlNode parentNode, string key)
        {
            try
            {
                TomlNode? textNode = parentNode[key];
                return textNode.IsString ? textNode.ToString() : null;
            }
            catch (NullReferenceException)
            {
                return null;
            }
        }

        public static bool? SafegetBool(TomlNode parentNode, string key)
        {
            try
            {
                TomlNode? boolNode = parentNode[key];
                return boolNode.IsBoolean ? boolNode : null;
            }
            catch (NullReferenceException)
            {
                return null;
            }
        }

        public static string[]? SafegetStringArray(TomlNode parentNode, string key, string[]? defaultValue = null)
        {
            try
            {
                TomlNode? arrayNode = parentNode[key];
                return arrayNode.IsArray
                    ? arrayNode.AsArray.RawArray.Select(x => x.AsString.Value).ToArray()
                    : defaultValue;
            }
            catch (NullReferenceException)
            {
                return defaultValue;
            }
        }

        public static TomlArray FromArray(string[] array)
        {
            var ret = new TomlArray();
            if (array == null)
            {
                return ret;
            }

            foreach (string? val in array)
            {
                ret.Add(val);
            }

            return ret;
        }
    }
}
