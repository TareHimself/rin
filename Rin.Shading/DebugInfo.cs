using System.Numerics;

namespace Rin.Shading;

public struct DebugInfo(string file, uint beginLine, uint beginCol, uint endLine, uint endCol)
    : IFormattable, IAdditionOperators<DebugInfo, DebugInfo, DebugInfo>
{
    public string File = file;
    public uint BeginLine = beginLine;
    public uint BeginCol = beginCol;
    public uint EndLine = endLine;
    public uint EndCol = endCol;

    public DebugInfo() : this("<unknown>", 0, 0)
    {
    }

    public DebugInfo(uint line, uint column) : this("<unknown>", line, column)
    {
    }

    public DebugInfo(string file, uint line, uint column) : this(file, line, column, line, column)
    {
    }

    public static DebugInfo operator +(DebugInfo left, DebugInfo right)
    {
        var minBeginLine = Math.Min(left.BeginLine, right.BeginLine);
        var minBeginCol = left.BeginLine == minBeginLine && right.BeginLine == minBeginLine
            ? Math.Min(left.BeginCol, right.BeginCol)
            : left.BeginLine == minBeginLine
                ? left.BeginCol
                : right.BeginCol;

        var maxEndLine = Math.Max(left.EndLine, right.EndLine);
        var maxEndCol = left.EndLine == maxEndLine && right.EndLine == maxEndLine
            ? Math.Max(left.EndCol, right.EndCol)
            : left.EndLine == maxEndLine
                ? left.EndCol
                : right.EndCol;

        return new DebugInfo(left.File, minBeginLine, minBeginCol, maxEndLine, maxEndCol);
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        FormattableString formattable =
            $"DebugInfo({nameof(File)}: {File}, {nameof(BeginLine)}: {BeginLine}, {nameof(EndLine)}: {EndLine}, {nameof(BeginCol)}: {BeginCol}, {nameof(EndCol)}: {EndCol})";
        return formattable.ToString(formatProvider);
    }

    public override string ToString()
    {
        return
            $"DebugInfo({nameof(File)}: {File}, {nameof(BeginLine)}: {BeginLine}, {nameof(EndLine)}: {EndLine}, {nameof(BeginCol)}: {BeginCol}, {nameof(EndCol)}: {EndCol})";
    }
}