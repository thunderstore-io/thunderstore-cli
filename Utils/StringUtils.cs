using System.Text.RegularExpressions;

namespace ThunderstoreCLI
{
    public static class StringUtils
    {
        /// <summary>
        /// Validate the given string adheres to MAJOR.MINOR.PATCH format
        /// </summary>
        public static bool IsSemVer(string version)
        {
            var regex = new Regex(@"^[0-9]+\.[0-9]+\.[0-9]+$");
            return regex.IsMatch(version);
        }
    }
}
