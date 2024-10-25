using System.Numerics;

namespace rsl;
public struct DebugInfo(string file, uint beginLine, uint beginCol,uint endLine,uint endCol) : IAdditionOperators<DebugInfo,DebugInfo,DebugInfo>
{
    public string File = file;
    public uint BeginLine = beginLine;
    public uint EndLine = beginCol;
    public uint BeginCol = endLine;
    public uint EndCol = endCol;

    public DebugInfo() : this("<unknown>", 0, 0)
    {
    }

    public DebugInfo(uint line, uint column) : this("<unknown>", line, column)
    {
    }
    
    public DebugInfo(string file, uint line, uint column) : this(file, line,column, line,column)
    {
    }

    public static DebugInfo operator +(DebugInfo left, DebugInfo right)
    {
        var minBeginLine = Math.Min(left.BeginLine, right.BeginLine);
        var minBeginCol = left.BeginLine == minBeginLine && right.BeginLine == minBeginLine
            ? Math.Min(left.BeginCol, right.BeginCol)
            : (left.BeginLine == minBeginLine ? left.BeginCol : right.BeginCol);
        
        var maxEndLine = Math.Max(left.EndLine, right.EndLine);
        var maxEndCol = left.EndLine == maxEndLine && right.EndLine == maxEndLine
            ? Math.Max(left.EndCol, right.EndCol)
            : (left.EndLine == maxEndLine ? left.EndCol : right.EndCol);

        return new DebugInfo(left.File, minBeginLine,minBeginCol, maxEndLine, maxEndCol);
    }
}