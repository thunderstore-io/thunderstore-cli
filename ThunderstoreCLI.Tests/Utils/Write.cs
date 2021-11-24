using System;
using System.IO;
using Xunit;

namespace ThunderstoreCLI.Tests;

public class FakeConsole : IDisposable
{
    private StringWriter _stringWriter = new StringWriter();
    private TextWriter _originalOutput = Console.Out;

    public FakeConsole()
    {
        Console.SetOut(_stringWriter);
    }

    public string GetOuput()
    {
        return _stringWriter.ToString();
    }

    public void Dispose()
    {
        Console.SetOut(_originalOutput);
        _stringWriter.Dispose();
    }
}

public class ThunderstoreCLI_Write
{
    // ANSI escape codes
    public static string Dim = "\u001b[2m";
    public static string Green = "\u001b[32m";
    public static string Red = "\u001b[31m";
    public static string Reset = "\u001b[0m";
    public static string Yellow = "\u001b[33m";

    public static string NL = Environment.NewLine;

    [Fact]
    public void Empty_WritesEmptyLine()
    {
        using var mockConsole = new FakeConsole();

        Write.Empty();

        Assert.Equal($"{NL}", mockConsole.GetOuput());
    }

    [Fact]
    public void Error_WritesErrorMessage()
    {
        using var mockConsole = new FakeConsole();

        Write.Error("MyError");

        Assert.Equal($"{Red}ERROR: MyError{Reset}{NL}", mockConsole.GetOuput());
    }

    [Fact]
    public void ErrorExit_WritesExtraLine()
    {
        using var mockConsole = new FakeConsole();

        Write.ErrorExit("MyError");

        Assert.Equal(
            $"{Red}ERROR: MyError{Reset}{NL}{Red}Exiting{Reset}{NL}",
            mockConsole.GetOuput()
        );
    }

    [Fact]
    public void Header_WritesAHeader()
    {
        using var mockConsole = new FakeConsole();

        Write.Header("Header");

        Assert.Equal($"{NL}Header{NL}------{NL}", mockConsole.GetOuput());
    }

    [Fact]
    public void Light_UsesDimmerColor()
    {
        using var mockConsole = new FakeConsole();

        Write.Light("Light");

        Assert.Equal($"{Dim}Light{Reset}{NL}", mockConsole.GetOuput());
    }

    [Fact]
    public void Line_UsesStandardColor()
    {
        using var mockConsole = new FakeConsole();

        Write.Line("Line");

        Assert.Equal($"Line{NL}", mockConsole.GetOuput());
    }

    [Fact]
    public void Note_UsesNotificationColor()
    {
        using var mockConsole = new FakeConsole();

        Write.Note("Note");

        Assert.Equal($"{Yellow}Note{Reset}{NL}", mockConsole.GetOuput());
    }

    [Fact]
    public void Success_UsesSuccessColor()
    {
        using var mockConsole = new FakeConsole();

        Write.Success("Great success!");

        Assert.Equal($"{Green}Great success!{Reset}{NL}", mockConsole.GetOuput());
    }

    [Fact]
    public void Warn_WritesWarningMessage()
    {
        using var mockConsole = new FakeConsole();

        Write.Warn("MyWarning");

        Assert.Equal($"{Yellow}WARNING: MyWarning{Reset}{NL}", mockConsole.GetOuput());
    }

    public static TheoryData<bool, bool, string> WithNewLines => new TheoryData<bool, bool, string>
    {
        { false, false, $"Line{NL}" },
        { true, false, $"{NL}Line{NL}" },
        { false, true, $"Line{NL}{NL}" },
        { true, true, $"{NL}Line{NL}{NL}" }
    };

    [Theory]
    [MemberData(nameof(WithNewLines))]
    public void WithNL_WritesNewLines(bool before, bool after, string expected)
    {
        using var mockConsole = new FakeConsole();

        Write.WithNL("Line", before, after);

        Assert.Equal(expected, mockConsole.GetOuput());
    }

    [Fact]
    public void MethodSupportingMultipleLines_WritesMultipleLines()
    {
        using var mockConsole = new FakeConsole();

        Write.Light("1", "2", "3");

        Assert.Equal(
            $"{Dim}1{Reset}{NL}{Dim}2{Reset}{NL}{Dim}3{Reset}{NL}",
            mockConsole.GetOuput()
        );
    }
}
