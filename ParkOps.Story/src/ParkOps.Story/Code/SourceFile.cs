using System.Reflection;

namespace ParkOps.Story.Code;

/// <summary>
/// A source file embedded at build time (see the csproj). <see cref="Lines"/> is what goes on screen:
/// the using/namespace preamble is dropped, since it's noise on a slide and differs between the
/// before (ParkOps.ControlRoom.Panels) and after (ParkOps.Story.Fixed) copies.
/// </summary>
public sealed class SourceFile
{
    private SourceFile(string name, string path, IReadOnlyList<string> lines, int firstLineNumber)
    {
        Name = name;
        Path = path;
        Lines = lines;
        FirstLineNumber = firstLineNumber;
    }

    /// <summary>File name shown on the tab, e.g. CensusPanel.cs.</summary>
    public string Name { get; }

    /// <summary>Path shown in the header, relative to the ParkOps solution.</summary>
    public string Path { get; }

    public IReadOnlyList<string> Lines { get; }

    /// <summary>The real line number of <c>Lines[0]</c> in the file, for the gutter.</summary>
    public int FirstLineNumber { get; }

    public static SourceFile Load(string resource, string path)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Embedded source '{resource}' is missing; check the csproj.");
        using var reader = new StreamReader(stream);
        var all = reader.ReadToEnd().ReplaceLineEndings("\n").TrimEnd('\n').Split('\n');

        var start = 0;
        while (start < all.Length && (all[start].StartsWith("using ") || all[start].StartsWith("namespace ") || all[start].Trim().Length == 0)) start++;

        return new(System.IO.Path.GetFileName(path), path, all[start..], start + 1);
    }

    /// <summary>The index in <see cref="Lines"/> of the first line containing <paramref name="text"/>.</summary>
    public int Find(string text)
    {
        for (var i = 0; i < Lines.Count; i++)
            if (Lines[i].Contains(text, StringComparison.Ordinal)) return i;
        throw new InvalidOperationException($"'{text}' not found in {Name}; the source changed under the story.");
    }
}
