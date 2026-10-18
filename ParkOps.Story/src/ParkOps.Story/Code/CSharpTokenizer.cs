namespace ParkOps.Story.Code;

public enum TokenKind { Text, Keyword, Type, Method, String, Comment, Number, Punctuation }

public readonly record struct Token(string Text, TokenKind Kind);

/// <summary>
/// Just enough C# lexing to highlight one line of a slide: keywords (including C# 15's contextual ones),
/// PascalCase types, method calls, strings with interpolation holes, comments and numbers.
/// </summary>
public static class CSharpTokenizer
{
    private static readonly HashSet<string> Keywords =
    [
        "abstract", "as", "async", "await", "base", "bool", "break", "case", "catch", "char", "class", "closed", "const",
        "continue", "default", "delegate", "do", "double", "else", "enum", "event", "extension", "false", "field", "finally",
        "for", "foreach", "get", "if", "in", "init", "int", "interface", "internal", "is", "long", "new", "not", "null",
        "object", "or", "and", "out", "override", "params", "partial", "private", "protected", "public", "readonly", "record",
        "ref", "required", "return", "sealed", "set", "static", "string", "struct", "switch", "this", "throw", "true", "try",
        "typeof", "union", "using", "var", "void", "when", "where", "while", "with", "yield",
    ];

    public static List<Token> Tokenize(string line)
    {
        List<Token> tokens = [];
        var i = 0;
        while (i < line.Length)
        {
            var c = line[i];
            if (char.IsWhiteSpace(c))
            {
                var start = i;
                while (i < line.Length && char.IsWhiteSpace(line[i])) i++;
                tokens.Add(new(line[start..i], TokenKind.Text));
            }
            else if (c == '/' && i + 1 < line.Length && line[i + 1] == '/')
            {
                tokens.Add(new(line[i..], TokenKind.Comment));
                break;
            }
            else if (c == '$' && i + 1 < line.Length && line[i + 1] == '"')
            {
                i = Interpolated(line, i, tokens);
            }
            else if (c == '"' || (c == '@' && i + 1 < line.Length && line[i + 1] == '"'))
            {
                var start = i;
                i += c == '@' ? 2 : 1;
                while (i < line.Length && line[i] != '"') i += line[i] == '\\' && c != '@' ? 2 : 1;
                i = Math.Min(line.Length, i + 1);
                tokens.Add(new(line[start..i], TokenKind.String));
            }
            else if (c == '\'')
            {
                var start = i++;
                while (i < line.Length && line[i] != '\'') i += line[i] == '\\' ? 2 : 1;
                i = Math.Min(line.Length, i + 1);
                tokens.Add(new(line[start..i], TokenKind.String));
            }
            else if (char.IsDigit(c))
            {
                var start = i;
                while (i < line.Length && (char.IsLetterOrDigit(line[i]) || line[i] is '.' or '_')) i++;
                tokens.Add(new(line[start..i], TokenKind.Number));
            }
            else if (char.IsLetter(c) || c == '_' || c == '@')
            {
                var start = i++;
                while (i < line.Length && (char.IsLetterOrDigit(line[i]) || line[i] == '_')) i++;
                var word = line[start..i];
                var next = i;
                while (next < line.Length && line[next] == ' ') next++;
                var kind = Keywords.Contains(word) ? TokenKind.Keyword
                    : next < line.Length && line[next] is '(' or '<' && char.IsUpper(word[0]) && !IsLikelyType(word, line, next) ? TokenKind.Method
                    : char.IsUpper(word[0]) ? TokenKind.Type
                    : TokenKind.Text;
                tokens.Add(new(word, kind));
            }
            else
            {
                tokens.Add(new(c.ToString(), TokenKind.Punctuation));
                i++;
            }
        }
        return tokens;
    }

    /// <summary><c>new Line(...)</c> and <c>List&lt;T&gt;</c> are types; <c>Render(...)</c> and <c>FullJoin(...)</c> are methods.</summary>
    private static bool IsLikelyType(string word, string line, int next)
    {
        if (line[next] == '<') return true;
        var before = line[..Math.Max(0, next - word.Length)].TrimEnd();
        return before.EndsWith("new");
    }

    /// <summary>$"...{hole}..." : literal parts as strings, holes as code.</summary>
    private static int Interpolated(string line, int i, List<Token> tokens)
    {
        var start = i;
        i += 2;
        while (i < line.Length && line[i] != '"')
        {
            if (line[i] == '{' && i + 1 < line.Length && line[i + 1] != '{')
            {
                tokens.Add(new(line[start..(i + 1)], TokenKind.String));
                var depth = 1;
                var holeStart = ++i;
                while (i < line.Length && depth > 0)
                {
                    if (line[i] == '{') depth++;
                    else if (line[i] == '}') depth--;
                    if (depth > 0) i++;
                }
                tokens.AddRange(Tokenize(line[holeStart..i]));
                start = i;
                i++;
            }
            else i += line[i] == '\\' ? 2 : 1;
        }
        i = Math.Min(line.Length, i + 1);
        tokens.Add(new(line[start..i], TokenKind.String));
        return i;
    }
}
