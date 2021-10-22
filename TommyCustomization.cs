using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tommy;
using System.Reflection;
using System.IO;

namespace ThunderstoreCLI
{

    // Tommy has it's TomlSyntax class marked as internal, so duplicate code
    // it is then.
    public static class TomlSyntax
    {
        public const char ARRAY_START_SYMBOL = '[';
        public const char ARRAY_END_SYMBOL = ']';
        public const char KEY_VALUE_SEPARATOR = '=';
        public const char NEWLINE_CHARACTER = '\n';
        public const char NEWLINE_CARRIAGE_RETURN_CHARACTER = '\r';
        public const char COMMENT_SYMBOL = '#';
        public const char BASIC_STRING_SYMBOL = '\"';

        public static void AsComment(this string self, TextWriter tw)
        {
            foreach (var line in self.Split(NEWLINE_CHARACTER))
                tw.WriteLine($"{COMMENT_SYMBOL} {line.Trim()}");
        }

        public static string AsKey(this string key)
        {
            var quote = key.Any(c => !IsBareKey(c));
            return !quote ? key : $"{BASIC_STRING_SYMBOL}{key.Escape()}{BASIC_STRING_SYMBOL}";
        }

        public static bool IsBareKey(char c) =>
            'A' <= c && c <= 'Z' || 'a' <= c && c <= 'z' || '0' <= c && c <= '9' || c == '_' || c == '-';

        public static bool IsNewLine(char c) => c == NEWLINE_CHARACTER || c == NEWLINE_CARRIAGE_RETURN_CHARACTER;

        public static bool ShouldBeEscaped(char c) => (c <= '\u001f' || c == '\u007f') && !IsNewLine(c);

        public static string Escape(this string txt, bool escapeNewlines = true)
        {
            var stringBuilder = new StringBuilder(txt.Length + 2);
            for (var i = 0; i < txt.Length; i++)
            {
                var c = txt[i];

                static string CodePoint(string txt, ref int i, char c) => char.IsSurrogatePair(txt, i)
                    ? $"\\U{char.ConvertToUtf32(txt, i++):X8}"
                    : $"\\u{(ushort)c:X4}";

                stringBuilder.Append(c switch
                {
                    '\b' => @"\b",
                    '\t' => @"\t",
                    '\n' when escapeNewlines => @"\n",
                    '\f' => @"\f",
                    '\r' when escapeNewlines => @"\r",
                    '\\' => @"\\",
                    '\"' => @"\""",
                    var _ when ShouldBeEscaped(c) || TOML.ForceASCII && c > sbyte.MaxValue =>
                        CodePoint(txt, ref i, c),
                    var _ => c
                });
            }

            return stringBuilder.ToString();
        }
    }

    public class FormattedTomlTable : TomlTable
    {
        // This snippet has been 95% copied from Tommy source code.
        // We simply care about fixing some of the formatting, but this seems
        // like the best way to do so without copying the entire source into
        // our project.
        public override void WriteTo(TextWriter tw, string name = null)
        {
            // The table is inline table
            if (IsInline && name != null)
            {
                tw.Write(ToInlineToml());
                return;
            }

            if (RawTable.All(n => n.Value.CollapseLevel != 0))
                return;

            var hasRealValues = !RawTable.All(n => n.Value is TomlTable tbl && !tbl.IsInline);


            // EDITS: We need to access the CollectCollapsedItems, which has
            // been nicely marked private for us. So use reflection to access.
            var method = typeof(TomlTable).GetMethod("CollectCollapsedItems", BindingFlags.NonPublic | BindingFlags.Instance);
            Dictionary<string, TomlNode> collapsedItems = (Dictionary<string, TomlNode>)method.Invoke(this, new object[] { null, "", null, 0 });
            // ORIGINAL:
            // var collapsedItems = this.CollectCollapsedItems(out var _);

            Comment?.AsComment(tw);

            if (name != null && (hasRealValues || collapsedItems.Count > 0))
            {
                tw.Write(TomlSyntax.ARRAY_START_SYMBOL);
                tw.Write(name);
                tw.Write(TomlSyntax.ARRAY_END_SYMBOL);
                tw.WriteLine();
            }
            else if (Comment != null) // Add some spacing between the first node and the comment
            {
                tw.WriteLine();
            }

            var namePrefix = name == null ? "" : $"{name}.";
            var first = true;

            var sectionableItems = new Dictionary<string, TomlNode>();

            foreach (var child in RawTable)
            {
                // If value should be parsed as section, separate if from the bunch
                if (child.Value is TomlArray arr && arr.IsTableArray || child.Value is TomlTable tbl && !tbl.IsInline)
                {
                    sectionableItems.Add(child.Key, child.Value);
                    continue;
                }

                // If the value is collapsed, it belongs to the parent
                if (child.Value.CollapseLevel != 0)
                    continue;

                // EDIT: Removed newline
                //if (!first) tw.WriteLine();
                first = false;

                var key = child.Key.AsKey();
                child.Value.Comment?.AsComment(tw);
                tw.Write(key);
                tw.Write(' ');
                tw.Write(TomlSyntax.KEY_VALUE_SEPARATOR);
                tw.Write(' ');

                child.Value.WriteTo(tw, $"{namePrefix}{key}");
            }

            foreach (var collapsedItem in collapsedItems)
            {
                if (collapsedItem.Value is TomlArray arr && arr.IsTableArray ||
                    collapsedItem.Value is TomlTable tbl && !tbl.IsInline)
                    throw new
                        TomlFormatException($"Value {collapsedItem.Key} cannot be defined as collapsed, because it is not an inline value!");

                tw.WriteLine();
                var key = collapsedItem.Key;
                collapsedItem.Value.Comment?.AsComment(tw);
                tw.Write(key);
                tw.Write(' ');
                tw.Write(TomlSyntax.KEY_VALUE_SEPARATOR);
                tw.Write(' ');

                collapsedItem.Value.WriteTo(tw, $"{namePrefix}{key}");
            }

            if (sectionableItems.Count == 0)
                return;

            tw.WriteLine();
            tw.WriteLine();
            first = true;
            foreach (var child in sectionableItems)
            {
                if (!first) tw.WriteLine();
                first = false;

                child.Value.WriteTo(tw, $"{namePrefix}{child.Key}");
            }
        }
    }
}
