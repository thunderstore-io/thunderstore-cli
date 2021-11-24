namespace ThunderstoreCLI.Comparers;

public class SemVer : IComparer<int[]>
{
    /// <summary>Compare two int arrays containing SemVer parts</summary>
    /// <remarks>Each parameter should have length of 3</remarks>
    public int Compare(int[]? a, int[]? b)
    {
        // IComparer forces us to accept nullable parameters, even
        // though in this case they make no sense.
        if (a is null && b is null)
        {
            return 0;
        }
        else if (a is null)
        {
            return -1;
        }
        else if (b is null)
        {
            return 1;
        }

        if (a[0] != b[0])
        {
            return a[0].CompareTo(b[0]);
        }

        if (a[1] != b[1])
        {
            return a[1].CompareTo(b[1]);
        }

        return a[2].CompareTo(b[2]);
    }
}
