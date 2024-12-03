namespace rsl;

public class ExceptionWithDebug : Exception
{
    DebugInfo Debug;
    public ExceptionWithDebug(DebugInfo debugInfo){
        Debug = debugInfo;
    }

    public ExceptionWithDebug(DebugInfo debugInfo,string message) : base(message){
        Debug = debugInfo;
    }
}
