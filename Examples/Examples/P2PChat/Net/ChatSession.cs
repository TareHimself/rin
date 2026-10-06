using System.Collections.Concurrent;

namespace Examples.P2PChat.Net;

/// <summary>
///     A chat connection that runs on background threads and hands events to the UI thread through <see cref="TryDequeue" />.
/// </summary>
public abstract class ChatSession(string name) : IDisposable
{
    public const int DefaultPort = 7777;
    private const int MaxNameLength = 24;

    private readonly ConcurrentQueue<ChatEvent> _events = [];

    protected CancellationTokenSource Cancellation { get; } = new();

    public string Name { get; } = CleanName(name);

    public bool TryDequeue(out ChatEvent chatEvent)
    {
        return _events.TryDequeue(out chatEvent!);
    }

    public void Send(string text)
    {
        text = text.Trim();
        if (text.Length > 0) _ = SendSafelyAsync(text);
    }

    public abstract Task SendAsync(string text);

    public virtual void Dispose()
    {
        Cancellation.Cancel();
    }

    protected void Publish(ChatEvent chatEvent)
    {
        _events.Enqueue(chatEvent);
    }

    protected static string CleanName(string name)
    {
        name = name.Trim();
        if (name.Length > MaxNameLength) name = name[..MaxNameLength];
        return name.Length == 0 ? "guest" : name;
    }

    private async Task SendSafelyAsync(string text)
    {
        try
        {
            await SendAsync(text);
        }
        catch (Exception e) when (e is IOException or ObjectDisposedException or OperationCanceledException)
        {
            Publish(new ChatClosed(e.Message));
        }
    }
}
