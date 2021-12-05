using System.IO;
using Tommy;
using Xunit;

namespace ThunderstoreCLI.Tests;

public class ThunderstoreCLI_TomlUtils
{
    public static string _key = "key";

    public static TheoryData<string> InvalidValues => new TheoryData<string>
    {
        $"{_key} = 0",
        $"{_key} = +3.1415",
        $"{_key} = -2E-2",
        $"{_key} = nan",
        $"{_key} = 1970-01-01T00:00:00Z",
        $"{_key} = 1970-01-01",
        $"{_key} = 00:00:00",
        $"{_key} = {{ x = 1, y = 2 }}"
    };

    public static TomlTable CreateTomlTable(string input)
    {
        using var reader = new StringReader(input);
        return TOML.Parse(reader);
    }

    [Fact]
    public void SafegetString_WhenKeyIsNotFound_ReturnsNull()
    {
        var table = CreateTomlTable("");

        var actual = TomlUtils.SafegetString(table, _key);

        Assert.Null(actual);
    }

    public static TheoryData<string, string> ValidStringValues => new TheoryData<string, string>
    {
        { $"{_key} = \"value\"", "value" },
        { $"{_key} = \"\"", "" },
        { $"foo = \"foo\"\n{_key} = \"value\"\nbar = \"bar\"", "value" },
        { $"{_key} = 'literal string'", "literal string" },
    };

    [Theory]
    [MemberData(nameof(ValidStringValues))]
    public void SafegetString_WhenValueIsString_ReturnsValue(string input, string expected)
    {
        var table = CreateTomlTable(input);

        var actual = TomlUtils.SafegetString(table, _key);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(InvalidValues))]
    [InlineData("key = true")]
    [InlineData("key = [\"foo\", \"bar\"]")]
    public void SafegetString_WhenValueIsNotString_ReturnsNull(string input)
    {
        var table = CreateTomlTable(input);

        var actual = TomlUtils.SafegetString(table, _key);

        Assert.Null(actual);
    }

    [Fact]
    public void SafegetBool_WhenKeyIsNotFound_ReturnsNull()
    {
        var table = CreateTomlTable("");

        var actual = TomlUtils.SafegetBool(table, _key);

        Assert.Null(actual);
    }

    public static TheoryData<string, bool> ValidBoolValues => new TheoryData<string, bool>
    {
        { $"{_key} = true", true },
        { $"{_key} = false", false }
    };

    [Theory]
    [MemberData(nameof(ValidBoolValues))]
    public void SafegetBool_WhenValueIsBool_ReturnsValue(string input, bool expected)
    {
        var table = CreateTomlTable(input);

        var actual = TomlUtils.SafegetBool(table, _key);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(InvalidValues))]
    [InlineData("key = \"true\"")]
    [InlineData("key = [\"foo\", \"bar\"]")]
    public void SafegetBool_WhenValueIsNotBool_ReturnsNull(string input)
    {
        var table = CreateTomlTable(input);

        var actual = TomlUtils.SafegetBool(table, _key);

        Assert.Null(actual);
    }

    [Fact]
    public void SafegetStringArray_WhenKeyIsNotFound_ReturnsNull()
    {
        var table = CreateTomlTable("");

        var actual = TomlUtils.SafegetBool(table, _key);

        Assert.Null(actual);
    }

    public static TheoryData<string, string[]> ValidStringArrayValues => new TheoryData<string, string[]>
    {
        { $"{_key} = []", new string[] { } },
        { $"{_key} = [\"\"]", new [] { "" } },
        { $"{_key} = [\"value\"]", new [] { "value" } },
        { $"{_key} = [\"value1\", \"value2\"]", new [] { "value1", "value2" } },
        { $"{_key} = ['literal']", new [] { "literal" } }
    };

    [Theory]
    [MemberData(nameof(ValidStringArrayValues))]
    public void SafegetStringArray_WhenValueIsStringArray_ReturnsValue(string input, string[] expected)
    {
        var table = CreateTomlTable(input);

        var actual = TomlUtils.SafegetStringArray(table, _key);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(InvalidValues))]
    [InlineData("key = \"value\"")]
    [InlineData("key = true")]
    public void SafegetStringArray_WhenValueIsNotStringArray_ReturnsNull(string input)
    {
        var table = CreateTomlTable(input);

        var actual = TomlUtils.SafegetStringArray(table, _key);

        Assert.Null(actual);
    }
}
