using System.Net.Sockets;
using System.Text.Json;

namespace Examples.P2PChat.Net;

/// <summary>
///     Connects to a <see cref="ChatHost" />. Messages, including your own, show up when the host relays them.
/// </summary>
public sealed class ChatClient(string name) : ChatSession(name)
{
    private ChatPeer? _host;

    public void Connect(string address, int port)
    {
        _ = Task.Run(() => RunAsync(address, port, Cancellation.Token));
    }

    public override async Task SendAsync(string text)
    {
        if (_host is { } host) await host.WriteAsync(new Frame(FrameKind.Chat, Name, text), Cancellation.Token);
    }

    public override void Dispose()
    {
        base.Dispose();
        _host?.Dispose();
    }

    private async Task RunAsync(string address, int port, CancellationToken cancellation)
    {
        try
        {
            var client = new TcpClient();
            await client.ConnectAsync(address, port, cancellation);
            var host = new ChatPeer(client);
            _host = host;
            await host.WriteAsync(new Frame(FrameKind.Hello, Name, string.Empty), cancellation);
            Publish(new ChatStarted($"Connected to {address}:{port}"));

            while (await FrameCodec.ReadAsync(host.Stream, cancellation) is { } frame)
                Publish(frame.Kind == FrameKind.Chat
                    ? new ChatMessage(frame.Sender, frame.Text)
                    : new ChatNotice(frame.Text));

            Publish(new ChatClosed("The host closed the connection"));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e) when (e is SocketException or IOException or ObjectDisposedException
                                      or InvalidDataException or JsonException)
        {
            Publish(new ChatClosed(e.Message));
        }
    }
}
