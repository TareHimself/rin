namespace Rin.Shading.Exceptions;

public class ParserException  : Exception
{
    public DebugInfo Debug;
    public ParserException(DebugInfo debugInfo){
        Debug = debugInfo;
    }

    public ParserException(DebugInfo debugInfo,string message) : base(message){
        Debug = debugInfo;
    }
}