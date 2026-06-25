using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Rin.Core.Generators;

public class SourceBuilder
{
    private readonly StringBuilder _builder = new();

    private int _indent;

    public SourceBuilder Indent()
    {
        _indent++;
        return this;
    }

    public SourceBuilder Outdent()
    {
        _indent--;
        _indent = int.Max(0, _indent);
        return this;
    }

    public SourceBuilder Line()
    {
        _builder.AppendLine();
        return this;
    }

    public SourceBuilder Line(string line)
    {
        _builder.AppendLine(string.Join("", Enumerable.Range(0, _indent).Select(_ => "\t")) + line);
        return this;
    }

    public SourceBuilder OpenBrace(string? prefix = null)
    {
        Line((prefix == null ? string.Empty : $"{prefix} ") + "{");
        Indent();
        return this;
    }

    public SourceBuilder CloseBrace()
    {
        Outdent();
        Line("}");
        return this;
    }

    public SourceText ToSourceText()
    {
        return SourceText.From(_builder.ToString(), Encoding.UTF8);
    }
}