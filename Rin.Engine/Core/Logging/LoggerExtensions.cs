namespace Rin.Engine.Core.Logging;

public static class LoggerExtensions
{
    public static ILogger AddConsole(this ILogger self)
    {
        return self.AddTransport(ConsoleTransport.Instance);
    }

    public static ILogger AddFile(this ILogger self, string filePath, bool async = false)
    {
        return self.AddTransport(new FileTransport(filePath, async));
    }
}