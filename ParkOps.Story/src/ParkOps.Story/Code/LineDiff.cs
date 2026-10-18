namespace ParkOps.Story.Code;

public enum Change { Same, Removed, Added }

public readonly record struct DiffLine(Change Change, string Text);

/// <summary>Line diff by longest common subsequence: plenty for slide-sized files. Removals come before additions in each hunk.</summary>
public static class LineDiff
{
    public static List<DiffLine> Compute(IReadOnlyList<string> before, IReadOnlyList<string> after)
    {
        var lcs = new int[before.Count + 1, after.Count + 1];
        for (var i = before.Count - 1; i >= 0; i--)
            for (var j = after.Count - 1; j >= 0; j--)
                lcs[i, j] = before[i] == after[j] ? lcs[i + 1, j + 1] + 1 : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);

        List<DiffLine> result = [];
        int b = 0, a = 0;
        while (b < before.Count || a < after.Count)
        {
            if (b < before.Count && a < after.Count && before[b] == after[a])
            {
                result.Add(new(Change.Same, before[b++]));
                a++;
            }
            else if (b < before.Count && (a == after.Count || lcs[b + 1, a] >= lcs[b, a + 1]))
                result.Add(new(Change.Removed, before[b++]));
            else
                result.Add(new(Change.Added, after[a++]));
        }
        return result;
    }
}
