using System.Net.Sockets;
using Examples.Common;
using P2PChat.Net;
using P2PChat.Views;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views;
using Rin.Core.Views.Composite;

namespace P2PChat;

public class P2PChatApplication(string[] args) : ExampleApplication
{
    private const int ConnectScreen = 0;
    private const int ChatScreen = 1;

    private ConnectView _connect = null!;
    private ChatRoomView _room = null!;
    private SwitcherView _screens = null!;
    private ChatSession? _session;

    protected override void OnStartup()
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

        ShowPreview();

        IGraphicsModule.Get().OnWindowRendererCreated += renderer =>
        {
            if (IViewsModule.Get().GetWindowSurface(renderer) is { } surface) surface.Add(_screens);
        };
        OnUpdate += _ => PumpEvents();
        IGraphicsModule.Get().OnWindowCreated += window => window.OnClose += _ => RequestExit();
        IGraphicsModule.Get().CreateWindow("P2P Chat", new Extent2D(900, 640),
            WindowFlags.Visible | WindowFlags.Resizable);
    }

    /// <summary>
    ///     Dev option: <c>--preview connect|join|chat</c> shows a screen with sample content and no networking.
    /// </summary>
    private void ShowPreview()
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

    protected override void OnShutdown()
    {
        _session?.Dispose();
        base.OnShutdown();
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
