using ParkOps.ControlRoom.Ui;
using ParkOps.Story.Rendering;
using SadRogue.Primitives;

namespace ParkOps.Story.Code;

public enum RowStyle { Normal, Flagged, Removed, Added, Settled }

/// <summary>
/// One source line as it should look right now: <paramref name="Reveal"/> characters typed so far,
/// the first <paramref name="Strike"/> characters struck through, and an optional gutter number.
/// </summary>
public sealed record CodeRow(string Text, RowStyle Style, int? Number, int Reveal = int.MaxValue, int Strike = 0, double Settle = 1);

/// <summary>A Leading EDJE-themed editor pane: file tab, line-number gutter, highlighted and soft-wrapped code.</summary>
public static class CodeView
{
    private const int GutterWidth = 7;

    /// <summary>Draws the pane's chrome and returns the area code goes in.</summary>
    public static Rectangle Window(Canvas canvas, Rectangle area, SourceFile file, string badge, Color badgeColor)
    {
        canvas.Fill(new Rectangle(area.X + 1, area.MaxExtentY + 1, area.Width, 1), Brand.Black, Brand.DarkGray, Cp437.LightShade);
        canvas.Fill(area, Brand.CodeText, Brand.CodeBackground);
        canvas.Fill(new Rectangle(area.X, area.Y, area.Width, 1), Brand.WhiteShade, Brand.Black);

        canvas.Glyph(area.X, area.Y, Glyphs.BlockLeft, Brand.Green, Brand.CodeBackground);
        canvas.Print(area.X + 1, area.Y, $" {file.Name} ", Brand.TrueWhite, Brand.CodeBackground);
        canvas.Print(area.X + file.Name.Length + 4, area.Y, file.Path, Brand.LineNumber, Brand.Black);

        var pill = $" {badge} ";
        canvas.Print(area.MaxExtentX - pill.Length, area.Y, pill, Brand.TrueWhite, badgeColor);

        canvas.Fill(new Rectangle(area.X, area.Y + 1, GutterWidth, area.Height - 1), Brand.LineNumber, Brand.CodeGutter);
        return new Rectangle(area.X, area.Y + 2, area.Width, area.Height - 3);
    }

    /// <summary>Draws the rows; returns the screen row each source row starts on (-1 if it didn't fit).</summary>
    public static int[] Draw(Canvas canvas, Rectangle area, IReadOnlyList<CodeRow> rows)
    {
        var starts = new int[rows.Count];
        var codeX = area.X + GutterWidth + 1;
        var width = area.MaxExtentX - codeX;
        var y = area.Y;

        foreach (var (index, row) in rows.Index())
        {
            starts[index] = y <= area.MaxExtentY ? y : -1;
            var (background, marker, markerColor) = row.Style switch
            {
                RowStyle.Removed => (Brand.RemovedBackground, '-', Brand.Red),
                RowStyle.Added => (Canvas.Lerp(Brand.AddedBackground, Brand.AddedSettled, row.Settle), '+', Brand.Green),
                RowStyle.Settled => (Brand.AddedSettled, '+', Brand.GreenShade),
                RowStyle.Flagged => (Brand.Flagged, Cp437.Play, Palette.Amber),
                _ => (Brand.CodeBackground, ' ', Brand.LineNumber),
            };

            // Lay the tokens out into cells, wrapping at the pane edge with a hanging indent.
            var indent = row.Text.Length - row.Text.TrimStart().Length + 4;
            List<List<(char Glyph, Color Color)>> segments = [[]];
            foreach (var token in CSharpTokenizer.Tokenize(row.Text))
            {
                foreach (var ch in token.Text)
                {
                    if (segments[^1].Count >= width)
                    {
                        segments.Add([.. Enumerable.Repeat((' ', Brand.CodeText), Math.Min(indent, width / 2))]);
                    }
                    segments[^1].Add((ch, ColorFor(token.Kind)));
                }
            }

            var typed = 0;
            var typing = false;
            foreach (var (part, cells) in segments.Index())
            {
                if (y > area.MaxExtentY || typing) break;
                canvas.Fill(new Rectangle(codeX - 1, y, area.MaxExtentX - codeX + 1, 1), Brand.CodeText, background);
                canvas.Fill(new Rectangle(area.X, y, GutterWidth, 1), Brand.LineNumber, row.Style == RowStyle.Normal ? Brand.CodeGutter : Canvas.Lerp(Brand.CodeGutter, background, 0.6));
                if (part == 0)
                {
                    if (row.Number is { } n) canvas.Print(area.X + 1, y, $"{n,4}", Brand.LineNumber);
                    canvas.Glyph(area.X + GutterWidth - 1, y, marker, markerColor);
                }
                else canvas.Glyph(area.X + GutterWidth - 1, y, Glyphs.Wrap, Brand.LineNumber);

                foreach (var (x, (glyph, color)) in cells.Index())
                {
                    if (part > 0 && x < Math.Min(indent, width / 2)) continue; // hanging indent
                    if (typed >= row.Reveal)
                    {
                        canvas.Glyph(codeX + x, y, Cp437.FullBlock, Brand.Green);
                        typing = true;
                        break;
                    }
                    var struck = typed < row.Strike;
                    canvas.Glyph(codeX + x, y, struck && glyph == ' ' ? Cp437.SingleHorizontal : glyph, struck ? Brand.Red : color);
                    typed++;
                }
                y++;
            }
        }
        return starts;
    }

    /// <summary>
    /// A leader that never crosses code: out of the card's left edge (the card must sit below the code), along to
    /// the gutter, up the gutter, and into the marker column of source row <paramref name="row"/>.
    /// </summary>
    public static Point[] GutterLeader(Rectangle codeArea, Rectangle box, int row)
    {
        var lane = codeArea.X + GutterWidth - 2;
        return [new(box.X - 1, box.Y + 1), new(lane, box.Y + 1), new(lane, row), new(lane + 1, row)];
    }

    public static Color ColorFor(TokenKind kind) => kind switch
    {
        TokenKind.Keyword => Brand.Keyword,
        TokenKind.Type => Brand.TypeName,
        TokenKind.Method => Brand.Method,
        TokenKind.String => Brand.StringLiteral,
        TokenKind.Comment => Brand.Comment,
        TokenKind.Number => Brand.Number,
        TokenKind.Punctuation => Brand.Punctuation,
        _ => Brand.CodeText,
    };
}
