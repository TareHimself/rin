using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace Examples.P2PChat.Net;

/// <summary>
///     Listens for peers and relays every chat message to all of them, including the sender.
/// </summary>
public sealed class ChatHost(string name) : ChatSession(name)
{
    private readonly List<ChatPeer> _peers = [];
    private TcpListener? _listener;

    public int Port { get; private set; }

    public void Start(int port)
    {
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        _listener = listener;
        Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Publish(new ChatStarted($"Hosting on port {Port}"));
        _ = Task.Run(() => AcceptLoopAsync(listener, Cancellation.Token));
    }

    public override async Task SendAsync(string text)
    {
        Publish(new ChatMessage(Name, text));
        await BroadcastAsync(new Frame(FrameKind.Chat, Name, text), Cancellation.Token);
    }

    public override void Dispose()
    {
        base.Dispose();
        _listener?.Stop();
        lock (_peers)
        {
            foreach (var peer in _peers) peer.Dispose();
            _peers.Clear();
        }
    }

    private async Task AcceptLoopAsync(TcpListener listener, CancellationToken cancellation)
    {
        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(cancellation);
                _ = Task.Run(() => ServePeerAsync(new ChatPeer(client), cancellation), cancellation);
            }
        }
        catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException or SocketException)
        {
        }
    }

    private async Task ServePeerAsync(ChatPeer peer, CancellationToken cancellation)
    {
        var joined = false;
        try
        {
            if (await FrameCodec.ReadAsync(peer.Stream, cancellation) is not { Kind: FrameKind.Hello } hello) return;

            peer.Name = CleanName(hello.Sender);
            lock (_peers) _peers.Add(peer);
            joined = true;
            await AnnounceAsync($"{peer.Name} joined", cancellation);

            while (await FrameCodec.ReadAsync(peer.Stream, cancellation) is { } frame)
            {
                if (frame.Kind != FrameKind.Chat || string.IsNullOrWhiteSpace(frame.Text)) continue;
                Publish(new ChatMessage(peer.Name, frame.Text));
                await BroadcastAsync(new Frame(FrameKind.Chat, peer.Name, frame.Text), cancellation);
            }
        }
        catch (Exception e) when (e is IOException or OperationCanceledException or ObjectDisposedException
                                      or InvalidDataException or JsonException)
        {
        }
        finally
        {
            lock (_peers) _peers.Remove(peer);
            peer.Dispose();
            if (joined && !cancellation.IsCancellationRequested)
                await AnnounceAsync($"{peer.Name} left", CancellationToken.None);
        }
    }

    private async Task AnnounceAsync(string text, CancellationToken cancellation)
    {
        Publish(new ChatNotice(text));
        await BroadcastAsync(new Frame(FrameKind.Notice, string.Empty, text), cancellation);
    }

    private async Task BroadcastAsync(Frame frame, CancellationToken cancellation)
    {
        ChatPeer[] peers;
        lock (_peers) peers = _peers.ToArray();

        foreach (var peer in peers)
        {
            try
            {
                await peer.WriteAsync(frame, cancellation);
            }
            catch (Exception e) when (e is IOException or ObjectDisposedException or OperationCanceledException)
            {
            }
        }
    }
}
