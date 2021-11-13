using Xunit;

namespace ThunderstoreCLI.Tests
{
    public class ThunderstoreCLI_Comparers_SemVer
    {
        private readonly Comparers.SemVer _semVer = new();

        public static TheoryData<int[], int[]> EqualValues => new()
        {
            { new[] { 1, 0, 0 }, new[] { 1, 0, 0 } },
            { new[] { 0, 1, 0 }, new[] { 0, 1, 0 } },
            { new[] { 0, 0, 1 }, new[] { 0, 0, 1 } },
            { new[] { 0, 0, 0 }, new[] { 0, 0, 0 } },
            { null, null }
        };

        [Theory]
        [MemberData(nameof(EqualValues))]
        public void ReturnZero_WhenValuesAreEqual(int[] a, int[] b)
        {
            int result = _semVer.Compare(a, b);

            Assert.Equal(0, result);
        }

        public static TheoryData<int[], int[]> GreaterValueFirst => new()
        {
            { new[] { 2, 0, 0 }, new[] { 1, 0, 0 } },
            { new[] { 1, 1, 0 }, new[] { 1, 0, 0 } },
            { new[] { 1, 0, 1 }, new[] { 1, 0, 0 } },
            { new[] { 0, 0, 1 }, new[] { 0, 0, 0 } },
            { new[] { 0, 0, 1 }, null }
        };

        [Theory]
        [MemberData(nameof(GreaterValueFirst))]
        public void ReturnOne_WhenGreaterValueIsFirst(int[] a, int[] b)
        {
            int result = _semVer.Compare(a, b);

            Assert.Equal(1, result);
        }

        public static TheoryData<int[], int[]> GreaterValueLast => new()
        {
            { new[] { 1, 0, 0 }, new[] { 2, 0, 0 } },
            { new[] { 1, 0, 0 }, new[] { 1, 1, 0 } },
            { new[] { 1, 0, 0 }, new[] { 1, 0, 1 } },
            { new[] { 0, 0, 0 }, new[] { 0, 0, 1 } },
            { null, new[] { 0, 0, 1 } }
        };

        [Theory]
        [MemberData(nameof(GreaterValueLast))]
        public void ReturnNegativeOne_WhenGreaterValueIsLast(int[] a, int[] b)
        {
            int result = _semVer.Compare(a, b);

            Assert.Equal(-1, result);
        }
    }
}
