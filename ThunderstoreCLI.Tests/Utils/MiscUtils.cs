using System;
using Xunit;

namespace ThunderstoreCLI.Tests;

public class ThunderstoreCLI_MiscUtils
{
    [Fact]
    public void GetCurrentVersion_WhenCalled_ReturnsCorrectNumberOfValues()
    {
        var actual = MiscUtils.GetCurrentVersion();

        // Since we can't know what the version actually is (without
        // duplicating most of the code inside GetCurrentVersion),
        // just test the method doesn't throw an exception.
        Assert.Equal(3, actual.Length);
    }

    [Fact]
    public void ParseLatestVersion_WhenFindsNoMatches_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => MiscUtils.ParseLatestVersion(""));
    }

    [Fact]
    public void ParseLatestVersion_WhenFindsOneMatch_ReturnsIt()
    {
        var actual = MiscUtils.ParseLatestVersion(@"[{""tag_name"":""1.0.0""}]");

        Assert.Equal(new[] { 1, 0, 0 }, actual);
    }

    public static TheoryData<string, int[]> MultipleReleases => new TheoryData<string, int[]>
    {
        { @"[{""tag_name"":""1.0.0""},{""tag_name"":""0.1.0""}]", new [] { 1, 0, 0 } },
        { @"[{""tag_name"":""1.9.9""},{""tag_name"":""2.0.0""}]", new [] { 2, 0, 0 } }
    };

    [Theory]
    [MemberData(nameof(MultipleReleases))]
    public void ParseLatestVersion_WhenFindsMultipleMatches_ReturnsLatest(string value, int[] expected)
    {
        var actual = MiscUtils.ParseLatestVersion(value);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ParseLatestVersion_WhenFindsPrerelease_IgnoresIt()
    {
        var actual = MiscUtils.ParseLatestVersion(@"[{""tag_name"":""2.0.0-alpha.1""},{""tag_name"":""1.0.0""}]");

        Assert.Equal(new[] { 1, 0, 0 }, actual);
    }
}
