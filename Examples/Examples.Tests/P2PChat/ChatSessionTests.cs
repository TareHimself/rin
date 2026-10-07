using Examples.P2PChat.Net;
using Examples.P2PChat.Views;

namespace Examples.Tests.P2PChat;

public class ChatSessionTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private ChatHost? _host;
    private ChatClient? _client;

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _host?.Dispose();
    }

    [Test]
    public async Task ClientMessageReachesTheHostAndIsEchoedBack()
    {
        await StartPair();

        _client!.Send("hi there");

        Assert.That(await WaitFor<ChatMessage>(_host!), Is.EqualTo(new ChatMessage("ann", "hi there")));
        Assert.That(await WaitFor<ChatMessage>(_client), Is.EqualTo(new ChatMessage("ann", "hi there")));
    }

    [Test]
    public async Task HostMessageReachesTheClient()
    {
        await StartPair();

        _host!.Send("welcome");

        Assert.That(await WaitFor<ChatMessage>(_client!), Is.EqualTo(new ChatMessage("hosty", "welcome")));
    }

    [Test]
    public async Task HostIsToldWhenAClientLeaves()
    {
        await StartPair();

        _client!.Dispose();

        Assert.That((await WaitFor<ChatNotice>(_host!, n => n.Text.EndsWith("left"))).Text, Is.EqualTo("ann left"));
    }

    [Test]
    public async Task ClientIsToldWhenTheHostGoesAway()
    {
        await StartPair();

        _host!.Dispose();

        Assert.That(await WaitFor<ChatClosed>(_client!), Is.Not.Null);
    }

    [Test]
    public async Task ConnectingToNothingReportsClosed()
    {
        var unused = new ChatHost("tmp");
        unused.Start(0);
        var port = unused.Port;
        unused.Dispose();
        _client = new ChatClient("ann");

        _client.Connect("127.0.0.1", port);

        Assert.That(await WaitFor<ChatClosed>(_client), Is.Not.Null);
    }

    [Test]
    public async Task BlankMessagesAreNotSent()
    {
        await StartPair();

        _client!.Send("   ");
        _client.Send("real");

        Assert.That((await WaitFor<ChatMessage>(_host!)).Text, Is.EqualTo("real"));
    }

    [TestCase("host", "host", 7777, true)]
    [TestCase("192.168.1.5:9000", "192.168.1.5", 9000, true)]
    [TestCase("  localhost:1  ", "localhost", 1, true)]
    [TestCase("host:abc", "", 0, false)]
    [TestCase("host:70000", "", 0, false)]
    [TestCase(":7777", "", 0, false)]
    [TestCase("", "", 0, false)]
    public void ParsesAddresses(string text, string host, int port, bool valid)
    {
        Assert.That(ConnectView.TryParseAddress(text, out var parsedHost, out var parsedPort), Is.EqualTo(valid));
        if (!valid) return;

        Assert.That(parsedHost, Is.EqualTo(host));
        Assert.That(parsedPort, Is.EqualTo(port));
    }

    private async Task StartPair()
    {
        _host = new ChatHost("hosty");
        _host.Start(0);
        _client = new ChatClient("ann");
        _client.Connect("127.0.0.1", _host.Port);
        await WaitFor<ChatStarted>(_client);
        await WaitFor<ChatNotice>(_host);
    }

    private static async Task<T> WaitFor<T>(ChatSession session, Func<T, bool>? match = null) where T : ChatEvent
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (DateTime.UtcNow < deadline)
        {
            while (session.TryDequeue(out var chatEvent))
                if (chatEvent is T typed && (match?.Invoke(typed) ?? true))
                    return typed;
            await Task.Delay(10);
        }

        throw new TimeoutException($"No {typeof(T).Name} within {Timeout}");
    }
}
