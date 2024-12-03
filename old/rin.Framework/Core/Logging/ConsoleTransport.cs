namespace rin.Framework.Core.Logging;

public class ConsoleTransport : ITransport
{
    
    public void Dispose()
    {
        // TODO release managed resources here
    }

    public void OnAdded(ILogger logger)
    {
    }

    public void Info(string message)
    {
        Console.Out.WriteLine(message);
    }

    public void Warn(string message)
    {
        Console.Out.WriteLine(message);
    }

    public void Error(string message, Exception? exception)
    {
        Console.Error.WriteLine(message);
    }

    public static ConsoleTransport Instance = new ConsoleTransport();
}