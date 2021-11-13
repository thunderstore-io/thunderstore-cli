using Xunit;

namespace ThunderstoreCLI.Tests
{
    public class ThunderstoreCLI_StringUtils
    {
        public static TheoryData<string> ValidSemVers => new()
        {
            "0.0.1",
            "0.1.0",
            "1.0.0",
            "134.546.789"
        };

        [Theory]
        [MemberData(nameof(ValidSemVers))]
        public void IsSemVer_WhenValueIsSemVer_ReturnsTrue(string value)
        {
            bool actual = StringUtils.IsSemVer(value);

            Assert.True(actual);
        }

        public static TheoryData<string> InvalidSemVers => new()
        {
            "1",
            "1.0",
            "1.0.",
            "1..0",
            "..",
            "1.0.a",
            "v1.0.0"
        };

        [Theory]
        [MemberData(nameof(InvalidSemVers))]
        public void IsSemVer_WhenValueIsNotSemVer_ReturnsFalse(string value)
        {
            bool actual = StringUtils.IsSemVer(value);

            Assert.False(actual);
        }

        public static TheoryData<string> UnsupportedValidSemVers => new()
        {
            "1.0.0-alpha.1",
            "1.0.0+build.1"
        };

        [Theory]
        [MemberData(nameof(UnsupportedValidSemVers))]
        public void IsSemVer_WhenValueIsNotSupported_ReturnsFalse(string value)
        {
            bool actual = StringUtils.IsSemVer(value);

            Assert.False(actual);
        }
    }
}
