using Rin.Core.Views;

namespace Examples.Editor.Views;

/// <summary>
///     The editor's colors and sizes, taken from its design mock rather than the shared example palette.
/// </summary>
public static class EditorTheme
{
    public static readonly Color Ground = Hex(0x0E1116);
    public static readonly Color Panel = Hex(0x151A21);
    public static readonly Color Card = Hex(0x1B212A);
    public static readonly Color Field = Hex(0x0E1116);
    public static readonly Color Viewport = Hex(0x0B0D11);
    public static readonly Color Border = Hex(0x2A313C);
    public static readonly Color BorderStrong = Hex(0x3A4250);
    public static readonly Color Hover = Hex(0x232A35);
    public static readonly Color Text = Hex(0xE6E9EE);
    public static readonly Color TextSoft = Hex(0xC3CAD4);
    public static readonly Color Muted = Hex(0x9AA4B2);
    public static readonly Color Faint = Hex(0x6F7A89);
    public static readonly Color AccentFill = Hex(0x24427F);
    public static readonly Color AccentBorder = Hex(0x3B6FD9);
    public static readonly Color Accent = Hex(0x4C8DFF);
    public static readonly Color Pill = Hex(0x1F2630);
    public static readonly Color LockFill = Hex(0x2B2414);
    public static readonly Color LockText = Hex(0xE8C46A);
    public static readonly Color AccentSoft = Hex(0x9FC0FF);
    public static readonly Color AxisX = Hex(0xF07C7C);
    public static readonly Color AxisY = Hex(0x7FD18B);
    public static readonly Color AxisZ = Hex(0x7FB0FF);

    public const float FontSize = 13f;
    public const float LabelWidth = 112f;
    public const float ControlRadius = 8f;
    public const float FieldRadius = 6f;
    public const float CardRadius = 10f;
    public const float FieldHeight = 28f;

    private static Color Hex(int rgb) =>
        new(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
}
