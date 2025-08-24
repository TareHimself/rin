namespace Rin.Framework.Logging;

public class ConsoleTransport : ITransport
{
    public static ConsoleTransport Instance = new();

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
}