using System.Numerics;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Layouts;

namespace P2PChat.Views;

/// <summary>
///     One chat message: a small name and time line above a bubble. Own messages sit on the right in the accent color.
/// </summary>
public sealed class MessageRowView : CompositeView
{
    private const float MaxBubbleFraction = 0.72f;
    private const float MaxBubbleWidth = 560f;
    private const float MetaGap = 4f;
    private const float BottomGap = 12f;

    private readonly bool _isOwn;
    private readonly TextBoxView _meta;
    private readonly CardView _bubble;
    private readonly ISlot[] _slots;

    public MessageRowView(string sender, string text, bool isOwn, DateTime time)
    {
        _isOwn = isOwn;
        var timeText = time.ToString("HH:mm");
        _meta = Ui.Label(isOwn ? timeText : $"{sender}  {timeText}", 13f, Ui.Muted);
        _bubble = new CardView
        {
            Color = isOwn ? Ui.Accent : Ui.Raised,
            BorderRadius = new Vector4(16f),
            Padding = new Padding(14f, 10f),
            InitChild = new TextBoxView
            {
                Content = text,
                FontSize = 17f,
                WrapContent = true,
                ForegroundColor = isOwn ? Color.White : Ui.Text
            }
        };
        _slots = [new SimpleSlot { Child = _meta }, new SimpleSlot { Child = _bubble }];
        _meta.SetParent(this);
        _bubble.SetParent(this);
    }

    public override ISlot[] GetSlots()
    {
        return _slots;
    }

    public override Vector2 ComputeDesiredContentSize()
    {
        var bubble = _bubble.GetDesiredSize();
        var meta = _meta.GetDesiredSize();
        return new Vector2(float.Max(bubble.X, meta.X), meta.Y + MetaGap + bubble.Y + BottomGap);
    }

    protected override Vector2 ArrangeContent(in Vector2 availableSpace)
    {
        var width = float.IsFinite(availableSpace.X) ? availableSpace.X : MaxBubbleWidth;
        var maxBubble = float.Min(width * MaxBubbleFraction, MaxBubbleWidth);

        var metaSize = _meta.Layout(new Vector2(width, float.PositiveInfinity));
        var bubbleSize = _bubble.Layout(new Vector2(maxBubble, float.PositiveInfinity));

        _meta.Offset = new Vector2(_isOwn ? width - metaSize.X : 0f, 0f);
        _bubble.Offset = new Vector2(_isOwn ? width - bubbleSize.X : 0f, metaSize.Y + MetaGap);
        return new Vector2(width, metaSize.Y + MetaGap + bubbleSize.Y + BottomGap);
    }
}
