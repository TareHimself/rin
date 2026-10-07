using System.Numerics;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Layouts;

namespace Examples.P2PChat.Views;

/// <summary>
///     The chat screen: status bar, scrolling messages and the input row.
/// </summary>
public sealed class ChatRoomView : RectView
{
    private const int MaxRows = 500;

    private readonly Queue<IView> _rows = [];
    private readonly ScrollListView _messages = new()
    {
        Axis = Axis.Column,
        Padding = new Padding(24f, 20f),
        BarColor = new Color(1f, 1f, 1f, 0.18f)
    };

    private readonly TextBoxView _status = Ui.Label(string.Empty, 16f);
    private readonly LineInputView _input = new() { Placeholder = "Write a message" };
    private bool _followTail = true;

    public ChatRoomView()
    {
        Color = Ui.Background;
        _input.Submitted += Submit;
        InitChild = Ui.Column(
            Ui.Slot(BuildHeader()),
            Ui.Slot(_messages, flex: 1),
            Ui.Slot(BuildInputRow()));
    }

    public event Action<string>? MessageSent;
    public event Action? LeaveRequested;

    public void Reset(string status)
    {
        while (_rows.Count > 0) _messages.Remove(_rows.Dequeue());
        _input.Content = string.Empty;
        _status.Content = status;
        _followTail = true;
    }

    public void AddMessage(string sender, string text, bool isOwn)
    {
        var row = new MessageRowView(sender, text, isOwn, DateTime.Now);
        AddRow(new ListSlot { Child = row, Fit = CrossFit.Available });
    }

    public void AddNotice(string text)
    {
        var notice = Ui.Label(text, 13f, Ui.Muted);
        notice.Padding = new Padding { Bottom = 12f };
        AddRow(new ListSlot { Child = notice, Align = CrossAlign.Center });
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        if (_followTail) _messages.ScrollTo(_messages.GetMaxScroll());
        _followTail = _messages.GetScroll() >= _messages.GetMaxScroll() - 1f;
    }

    private void AddRow(ListSlot slot)
    {
        _rows.Enqueue(slot.Child);
        _messages.Add(slot);
        while (_rows.Count > MaxRows) _messages.Remove(_rows.Dequeue());
    }

    private CardView BuildHeader()
    {
        return new CardView
        {
            Color = Ui.Surface,
            Padding = new Padding(24f, 16f),
            InitChild = Ui.Row(
                Ui.Slot(new StatusDot(), fit: CrossFit.Desired, align: CrossAlign.Center),
                Ui.Slot(Ui.Gap(0f, 10f)),
                Ui.Slot(_status, flex: 1, fit: CrossFit.Desired, align: CrossAlign.Center),
                Ui.Slot(Leave(), fit: CrossFit.Desired, align: CrossAlign.Center))
        };
    }

    private ActionButton Leave()
    {
        var button = Ui.DangerButton("Leave", () => LeaveRequested?.Invoke());
        button.Padding = new Padding(16f, 8f);
        return button;
    }

    private CardView BuildInputRow()
    {
        var send = Ui.PrimaryButton("Send", Submit);
        return new CardView
        {
            Color = Ui.Surface,
            Padding = new Padding(24f, 16f),
            InitChild = Ui.Row(
                Ui.Slot(Ui.Input(_input, 24f), flex: 1),
                Ui.Slot(Ui.Gap(0f, 12f)),
                Ui.Slot(send, fit: CrossFit.Fill))
        };
    }

    private void Submit()
    {
        var text = _input.Content.Trim();
        _input.Content = string.Empty;
        if (text.Length > 0) MessageSent?.Invoke(text);
    }
}
