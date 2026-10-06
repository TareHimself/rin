using System.Numerics;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Layouts;

namespace P2PChat.Views;

public static class Ui
{
    public static readonly Color Background = new(0.055f, 0.063f, 0.078f, 1f);
    public static readonly Color Surface = new(0.09f, 0.10f, 0.125f, 1f);
    public static readonly Color Raised = new(0.125f, 0.14f, 0.175f, 1f);
    public static readonly Color Border = new(0.19f, 0.21f, 0.26f, 1f);
    public static readonly Color Text = new(0.93f, 0.94f, 0.97f, 1f);
    public static readonly Color Muted = new(0.55f, 0.58f, 0.67f, 1f);
    public static readonly Color Accent = new(0.42f, 0.48f, 1f, 1f);
    public static readonly Color AccentHover = new(0.52f, 0.58f, 1f, 1f);
    public static readonly Color AccentPressed = new(0.34f, 0.39f, 0.9f, 1f);
    public static readonly Color Success = new(0.24f, 0.84f, 0.55f, 1f);
    public static readonly Color Danger = new(1f, 0.42f, 0.45f, 1f);
    public static readonly Color DangerFill = new(0.26f, 0.12f, 0.15f, 1f);
    public static readonly Color DangerFillHover = new(0.36f, 0.15f, 0.19f, 1f);
    public static readonly Color DangerFillPressed = new(0.2f, 0.09f, 0.12f, 1f);
    public static readonly Color Clear = new(0f, 0f, 0f, 0f);

    public static TextBoxView Label(string text, float size = 16f, Color? color = null)
    {
        return new TextBoxView { Content = text, FontSize = size, ForegroundColor = color ?? Text };
    }

    public static SpacerView Gap(float height, float width = 0f)
    {
        return new SpacerView { VerticalSpacing = height, HorizontalSpacing = width };
    }

    public static FlexBoxSlot Slot(IView child, float? flex = null, CrossFit fit = CrossFit.Fill,
        CrossAlign align = CrossAlign.Start)
    {
        return new FlexBoxSlot { Child = child, Flex = flex, Fit = fit, Align = align };
    }

    public static FlexBoxView Column(params FlexBoxSlot[] slots)
    {
        return new FlexBoxView { Axis = Axis.Column, InitSlots = slots };
    }

    public static FlexBoxView Row(params FlexBoxSlot[] slots)
    {
        return new FlexBoxView { Axis = Axis.Row, InitSlots = slots };
    }

    public static InputCard Input(LineInputView input, float radius = 12f)
    {
        return new InputCard(input, radius);
    }

    public static ActionButton PrimaryButton(string text, Action onClicked)
    {
        return Button(text, onClicked, Accent, AccentHover, AccentPressed, Color.White);
    }

    public static ActionButton DangerButton(string text, Action onClicked)
    {
        return Button(text, onClicked, DangerFill, DangerFillHover, DangerFillPressed, Danger);
    }

    private static ActionButton Button(string text, Action onClicked, Color normal, Color hover, Color pressed,
        Color textColor)
    {
        var button = new ActionButton(text, normal, hover, pressed, textColor);
        button.Clicked += onClicked;
        return button;
    }
}
