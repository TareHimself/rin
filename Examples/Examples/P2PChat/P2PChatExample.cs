using System.Net.Sockets;
using Examples.Common;
using Examples.P2PChat.Net;
using Examples.P2PChat.Views;
using Rin.Core.Graphics;
using Rin.Core.Views.Composite;

namespace Examples.P2PChat;

public sealed class P2PChatExample : Example
{
    private const int ConnectScreen = 0;
    private const int ChatScreen = 1;

    private ConnectView _connect = null!;
    private ChatRoomView _room = null!;
    private SwitcherView _screens = null!;
    private ChatSession? _session;

    public override string Name => "p2p-chat";

    public override string Title => "P2P Chat";

    public override Extent2D WindowSize => new(900, 640);

    public override void Start(ExampleContext context)
    {
        _connect = new ConnectView();
        _room = new ChatRoomView();
        _screens = new SwitcherView();
        _screens.Add(_connect);
        _screens.Add(_room);
        _connect.HostRequested += Host;
        _connect.ConnectRequested += Connect;
        _room.MessageSent += text => _session?.Send(text);
        _room.LeaveRequested += () => Leave(null);

        ShowPreview(context.Args);

        context.Surface.Add(_screens);
        context.Application.OnUpdate += _ => PumpEvents();
    }

    /// <summary>
    ///     Dev option: <c>--preview connect|join|chat</c> shows a screen with sample content and no networking.
    /// </summary>
    private void ShowPreview(string[] args)
    {
        var index = Array.IndexOf(args, "--preview");
        if (index < 0 || index + 1 >= args.Length) return;

        switch (args[index + 1])
        {
            case "join":
                _connect.SetMode(false);
                break;
            case "chat":
                _room.Reset("Hosting on port 7777");
                _room.AddNotice("ann joined");
                _room.AddMessage("ann", "hey! can you see this?", false);
                _room.AddMessage("you", "yep, loud and clear", true);
                _room.AddMessage("ann", "this one is a much longer message so we can check that bubbles wrap nicely instead of stretching across the whole window", false);
                _room.AddMessage("you", "looks good to me", true);
                _screens.SelectedIndex = ChatScreen;
                break;
        }
    }

    public override void Stop()
    {
        _session?.Dispose();
    }

    private void Host(string name, int port)
    {
        var host = new ChatHost(name);
        try
        {
            host.Start(port);
        }
        catch (SocketException e)
        {
            host.Dispose();
            _connect.SetStatus(e.Message, true);
            return;
        }

        _session?.Dispose();
        _session = host;
    }

    private void Connect(string name, string address, int port)
    {
        _session?.Dispose();
        var client = new ChatClient(name);
        _session = client;
        client.Connect(address, port);
    }

    private void Leave(string? reason)
    {
        _session?.Dispose();
        _session = null;
        _screens.SelectedIndex = ConnectScreen;
        _connect.SetStatus(reason ?? string.Empty, reason != null);
    }

    private void PumpEvents()
    {
        while (_session is { } session && session.TryDequeue(out var chatEvent))
            switch (chatEvent)
            {
                case ChatStarted started:
                    _room.Reset(started.Description);
                    _screens.SelectedIndex = ChatScreen;
                    break;
                case ChatMessage message:
                    _room.AddMessage(message.Sender, message.Text, message.Sender == session.Name);
                    break;
                case ChatNotice notice:
                    _room.AddNotice(notice.Text);
                    break;
                case ChatClosed closed:
                    Leave(closed.Reason);
                    break;
            }
    }
}
