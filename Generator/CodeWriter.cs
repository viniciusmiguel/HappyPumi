using System.Text;

namespace PulumiApiGenerator;

/// <summary>Simple indent-aware code writer.</summary>
public sealed class CodeWriter
{
    private readonly StringBuilder _sb = new();
    private int _indent;
    public string IndentString { get; init; } = "    ";

    public CodeWriter Line() { _sb.AppendLine(); return this; }

    public CodeWriter Line(string text)
    {
        if (text.Length == 0) { _sb.AppendLine(); return this; }
        for (int i = 0; i < _indent; i++) _sb.Append(IndentString);
        _sb.AppendLine(text);
        return this;
    }

    public CodeWriter Lines(params string[] lines)
    {
        foreach (var l in lines) Line(l);
        return this;
    }

    /// <summary>Emit a `{ ... }` block. Returns an IDisposable that closes the brace.</summary>
    public IDisposable Block(string header)
    {
        Line(header);
        Line("{");
        _indent++;
        return new BraceCloser(this);
    }

    /// <summary>Emit a /// xml doc comment from a possibly-multiline description.</summary>
    public CodeWriter XmlDoc(string? description)
    {
        if (string.IsNullOrWhiteSpace(description)) return this;
        Line("/// <summary>");
        foreach (var ln in description.Replace("\r\n", "\n").Split('\n'))
        {
            var safe = System.Security.SecurityElement.Escape(ln) ?? string.Empty;
            Line($"/// {safe}");
        }
        Line("/// </summary>");
        return this;
    }

    public override string ToString() => _sb.ToString();

    private sealed class BraceCloser : IDisposable
    {
        private readonly CodeWriter _w;
        public BraceCloser(CodeWriter w) { _w = w; }
        public void Dispose() { _w._indent--; _w.Line("}"); }
    }
}
