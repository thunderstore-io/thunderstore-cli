using System.Text.RegularExpressions;

namespace ThunderstoreCLI.Utils;

public static class StringUtils
{
    private static readonly Regex SemVerRegex = new(@"^[0-9]+\.[0-9]+\.[0-9]+$");

    /// <summary>
    /// Validate the given string adheres to MAJOR.MINOR.PATCH format
    /// </summary>
    /// <remarks>
    /// Prerelease and build postfixes are not supported and will
    /// return false.
    /// </remarks>
    public static bool IsSemVer(string version)
    {
        return SemVerRegex.IsMatch(version);
    }
}
