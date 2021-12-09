using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using NuGet.Frameworks;
using ThunderstoreCLI.Models;
using Xunit;

namespace ThunderstoreCLI.Tests;

public class TestJson : BaseJson<TestJson, TestJsonContext>
{
    public class Location
    {
        public string address { get; set; }
    }

    public string name { get; set; }
    public int age { get; set; }
    public Location home { get; set; }
}

[JsonSerializable(typeof(TestJson))]
public partial class TestJsonContext : JsonSerializerContext { }

public class ThunderstoreCLI_BaseJson
{
    [Fact]
    public void Deserialize_WhenGivenEmpty_ReturnsDefaults()
    {
        var test = TestJson.Deserialize("{}");

        Assert.NotNull(test);

        Assert.Null(test.name);
        Assert.Equal(0, test.age);
        Assert.Null(test.home);
    }

    [Fact]
    public void Deserialize_WhenGivenFilled_ReturnsExpected()
    {
        var test = TestJson.Deserialize(@"
{
    ""name"": ""john"",
    ""age"": 24,
    ""home"": {
        ""address"": ""123 house street""
    }
}"
        );

        Assert.NotNull(test);
        Assert.NotNull(test.home);

        Assert.Equal("john", test.name);
        Assert.Equal(24, test.age);
        Assert.Equal("123 house street", test.home.address);
    }

    [Fact]
    public void Serialize_WhenGivenFilled_ReturnsExpected()
    {
        Assert.Equal(
            @"{""name"":""smith"",""age"":32,""home"":{""address"":""456 business parkway""}}",
            new TestJson()
            {
                name = "smith",
                age = 32,
                home = new TestJson.Location()
                {
                    address = "456 business parkway"
                }
            }.Serialize()
        );
    }

    [Fact]
    public void Serialize_WhenAskedToIndent_Indents()
    {
        var options = new JsonSerializerOptions()
        {
            WriteIndented = true
        };

        Assert.Equal(
            @"{
  ""name"": ""jason"",
  ""age"": 19,
  ""home"": {
    ""address"": ""nowhere""
  }
}",
            new TestJson()
            {
                name = "jason",
                age = 19,
                home = new TestJson.Location()
                {
                    address = "nowhere"
                }
            }.Serialize(options)
        );
    }
}
