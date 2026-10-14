using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

/// <summary>
/// An indentation-aware text builder for Slang source that remembers, per emitted line, the C# location
/// it came from (<see cref="Origin" /> at the time the line was written).
/// </summary>
internal sealed class SlangWriter
{
    private readonly StringBuilder _builder = new();
    private readonly List<Location?> _origins = [];
    private int _indent;

    /// <summary>
    /// The C# location recorded for every line written until it is changed again.
    /// </summary>
    public Location? Origin { get; set; }

    public SlangWriter Line()
    {
        _builder.Append('\n');
        _origins.Add(Origin);
        return this;
    }

    public SlangWriter Line(string line)
    {
        _builder.Append(string.Concat(Enumerable.Repeat("    ", _indent))).Append(line).Append('\n');
        _origins.Add(Origin);
        return this;
    }

    public SlangWriter OpenBrace(string? prefix = null)
    {
        if (prefix is not null) Line(prefix);
        Line("{");
        _indent++;
        return this;
    }

    public SlangWriter CloseBrace()
    {
        _indent = int.Max(0, _indent - 1);
        Line("}");
        return this;
    }

    /// <summary>
    /// Appends raw text whose lines all map to the current <see cref="Origin" />.
    /// </summary>
    public SlangWriter Append(string text)
    {
        _builder.Append(text);
        for (var i = 0; i < text.Length; i++)
            if (text[i] == '\n')
                _origins.Add(Origin);
        return this;
    }

    /// <summary>
    /// Appends the text of another writer, keeping the origin of each of its lines.
    /// </summary>
    public SlangWriter Append(SlangWriter other)
    {
        _builder.Append(other._builder);
        _origins.AddRange(other._origins);
        return this;
    }

    /// <summary>
    /// Appends already-formatted lines, each re-indented to the current depth and mapped to the current <see cref="Origin" />.
    /// </summary>
    public SlangWriter AppendBlock(string text)
    {
        var indent = string.Concat(Enumerable.Repeat("    ", _indent));
        foreach (var line in text.TrimEnd('\n').Split('\n'))
        {
            _builder.Append(line.Length == 0 ? "" : indent + line).Append('\n');
            _origins.Add(Origin);
        }

        return this;
    }

    /// <summary>
    /// Appends the lines of another writer, each re-indented to the current depth and keeping its origin.
    /// </summary>
    public SlangWriter AppendBlock(SlangWriter other)
    {
        var indent = string.Concat(Enumerable.Repeat("    ", _indent));
        var lines = other._builder.ToString().TrimEnd('\n').Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            _builder.Append(lines[i].Length == 0 ? "" : indent + lines[i]).Append('\n');
            _origins.Add(i < other._origins.Count ? other._origins[i] : null);
        }

        return this;
    }

    public bool IsEmpty => _builder.Length == 0;

    public SlangSourceMap ToSourceMap() => new(_origins);

    public override string ToString() => _builder.ToString();
}
