﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ThunderstoreCLI.Config;
using Tommy;

namespace ThunderstoreCLI
{
    public static class TomlUtils
    {
        public static FormattedTomlTable DictToTomlTable(Dictionary<string, string> dict)
        {
            var result = new FormattedTomlTable();
            foreach (var kvp in dict)
            {
                result.Add(kvp.Key, kvp.Value);
            }
            return result;
        }

        public static TomlArray BuildCopyPathTable(List<CopyPathMap> list)
        {
            var result = new TomlArray() { IsTableArray = true };
            foreach (var entry in list)
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
            using (var writer = new StringWriter())
            {
                writer.NewLine = "\n";
                toml.WriteTo(writer);
                writer.Flush();
                return writer.ToString().Trim();
            }
        }

        public static string SafegetString(TomlNode node, string key)
        {
            try
            {
                return node[key];
            }
            catch (NullReferenceException)
            {
                return null;
            }
        }

        public static bool? SafegetBool(TomlNode node, string key)
        {
            try
            {
                return node[key];
            }
            catch (NullReferenceException)
            {
                return null;
            }
        }
    }
}
