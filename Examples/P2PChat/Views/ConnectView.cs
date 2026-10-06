using System.Numerics;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Layouts;
using P2PChat.Net;

namespace P2PChat.Views;

/// <summary>
///     The start screen: pick a name, then either host on a port or join a host's address.
/// </summary>
public sealed class ConnectView : RectView
{
    private const float CardWidth = 440f;

    private readonly LineInputView _name = new() { Content = "guest", Placeholder = "Your name" };
    private readonly LineInputView _port = new() { Content = ChatSession.DefaultPort.ToString(), Placeholder = "7777" };
    private readonly LineInputView _address = new()
    {
        Content = $"127.0.0.1:{ChatSession.DefaultPort}",
        Placeholder = "host or host:port"
    };

    private readonly TextBoxView _status = Ui.Label(string.Empty, 14f, Ui.Muted);
    private readonly ActionButton _hostTab;
    private readonly ActionButton _joinTab;
    private readonly ActionButton _go;
    private readonly FlexBoxView _hostSection;
    private readonly FlexBoxView _joinSection;
    private bool _hosting = true;

    public ConnectView()
    {
        Color = Ui.Background;
        _hostTab = Tab("Host a room", () => SetMode(true));
        _joinTab = Tab("Join a room", () => SetMode(false));
        _go = Ui.PrimaryButton("Start hosting", Go);
        _hostSection = Section("Port", _port, $"Friends on your network can join with {LocalAddress.Find()}");
        _joinSection = Section("Host address", _address, "Ask the host for their address and port");
        SetMode(true);

        InitChild = new PanelView
        {
            InitSlots =
            [
                new PanelSlot
                {
                    Child = BuildCard(),
                    MinAnchor = new Vector2(0.5f),
                    MaxAnchor = new Vector2(0.5f),
                    Alignment = new Vector2(0.5f),
                    SizeToContent = true
                }
            ]
        };
    }

    public event Action<string, int>? HostRequested;
    public event Action<string, string, int>? ConnectRequested;

    public void SetMode(bool hosting)
    {
        _hosting = hosting;
        _hostSection.Visibility = hosting ? Visibility.Visible : Visibility.Collapsed;
        _joinSection.Visibility = hosting ? Visibility.Collapsed : Visibility.Visible;
        _go.SetText(hosting ? "Start hosting" : "Connect");
        StyleTab(_hostTab, hosting);
        StyleTab(_joinTab, !hosting);
        SetStatus(string.Empty, false);
    }

    public void SetStatus(string text, bool isError)
    {
        _status.Content = text;
        _status.ForegroundColor = isError ? Ui.Danger : Ui.Muted;
    }

    private CardView BuildCard()
    {
        return new CardView
        {
            Color = Ui.Surface,
            BorderColor = Ui.Border,
            ShadowAlpha = 0.5f,
            BorderRadius = new Vector4(22f),
            Padding = new Padding(36f),
            InitChild = new SizerView
            {
                WidthOverride = CardWidth,
                InitChild = Ui.Column(
                    Ui.Slot(Ui.Label("P2P Chat", 36f)),
                    Ui.Slot(Ui.Gap(8f)),
                    Ui.Slot(Ui.Label("Chat directly with a friend. No server needed.", 15f, Ui.Muted)),
                    Ui.Slot(Ui.Gap(28f)),
                    Ui.Slot(BuildTabs()),
                    Ui.Slot(Ui.Gap(24f)),
                    Ui.Slot(Section("Display name", _name, null)),
                    Ui.Slot(_hostSection),
                    Ui.Slot(_joinSection),
                    Ui.Slot(Ui.Gap(26f)),
                    Ui.Slot(_go),
                    Ui.Slot(Ui.Gap(14f)),
                    Ui.Slot(_status))
            }
        };
    }

    private CardView BuildTabs()
    {
        return new CardView
        {
            Color = Ui.Background,
            BorderColor = Ui.Border,
            BorderRadius = new Vector4(14f),
            Padding = new Padding(4f),
            InitChild = Ui.Row(
                Ui.Slot(_hostTab, flex: 1),
                Ui.Slot(Ui.Gap(0f, 4f)),
                Ui.Slot(_joinTab, flex: 1))
        };
    }

    private static ActionButton Tab(string text, Action onClicked)
    {
        var tab = new ActionButton(text, Ui.Clear, Ui.Raised, Ui.Raised, Ui.Muted, 16f) { };
        tab.Clicked += onClicked;
        return tab;
    }

    private static void StyleTab(ActionButton tab, bool selected)
    {
        if (selected)
            tab.Restyle(Ui.Accent, Ui.Accent, Ui.AccentPressed, Color.White);
        else
            tab.Restyle(Ui.Clear, Ui.Raised, Ui.Raised, Ui.Muted);
    }

    private static FlexBoxView Section(string label, LineInputView input, string? hint)
    {
        List<FlexBoxSlot> slots =
        [
            Ui.Slot(Ui.Label(label, 13f, Ui.Muted)),
            Ui.Slot(Ui.Gap(8f)),
            Ui.Slot(Ui.Input(input))
        ];
        if (hint != null)
        {
            slots.Add(Ui.Slot(Ui.Gap(8f)));
            slots.Add(Ui.Slot(Ui.Label(hint, 13f, Ui.Muted)));
        }

        slots.Add(Ui.Slot(Ui.Gap(18f)));
        return new FlexBoxView { Axis = Axis.Column, InitSlots = slots.ToArray() };
    }

    private void Go()
    {
        if (_hosting)
            Host();
        else
            Connect();
    }

    private void Host()
    {
        if (!int.TryParse(_port.Content, out var port) || port is < 0 or > 65535)
        {
            SetStatus("Port must be a number from 0 to 65535", true);
            return;
        }

        SetStatus("Starting host...", false);
        HostRequested?.Invoke(_name.Content, port);
    }

    private void Connect()
    {
        if (!TryParseAddress(_address.Content, out var host, out var port))
        {
            SetStatus("Address must look like host or host:port", true);
            return;
        }

        SetStatus($"Connecting to {host}:{port}...", false);
        ConnectRequested?.Invoke(_name.Content, host, port);
    }

    public static bool TryParseAddress(string text, out string host, out int port)
    {
        text = text.Trim();
        host = text;
        port = ChatSession.DefaultPort;

        var colon = text.LastIndexOf(':');
        if (colon >= 0)
        {
            host = text[..colon];
            if (!int.TryParse(text[(colon + 1)..], out port) || port is < 1 or > 65535) return false;
        }

        return host.Length > 0;
    }
}
