using System.Net.Sockets;

namespace Examples.P2PChat.Net;

public sealed class ChatPeer(TcpClient client) : IDisposable
{
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public NetworkStream Stream { get; } = client.GetStream();
    public string Name { get; set; } = string.Empty;

    public async Task WriteAsync(Frame frame, CancellationToken cancellation)
    {
        await _writeLock.WaitAsync(cancellation);
        try
        {
            await FrameCodec.WriteAsync(Stream, frame, cancellation);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public void Dispose()
    {
        client.Dispose();
    }
}
