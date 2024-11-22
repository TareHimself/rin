namespace rin.Core.Logging;

public static class LoggerExtensions
{
    public static ILogger AddConsole(this ILogger self) => self.AddTransport(ConsoleTransport.Instance);

    public static ILogger AddFile(this ILogger self, string filePath, bool async = false) =>
        self.AddTransport(new FileTransport(filePath, async));
}