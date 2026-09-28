using System.Linq;
using System.Text;

namespace Rin.Shade.Transpiler;

internal sealed class SlangWriter
{
    private readonly StringBuilder _builder = new();
    private int _indent;

    public SlangWriter Line()
    {
        _builder.Append('\n');
        return this;
    }

    public SlangWriter Line(string line)
    {
        _builder.Append(string.Concat(Enumerable.Repeat("    ", _indent))).Append(line).Append('\n');
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

    public SlangWriter Append(string text)
    {
        _builder.Append(text);
        return this;
    }

    public override string ToString() => _builder.ToString();
}
