namespace Rin.Engine.Graphics.Windows;

[Flags]
public enum WindowFlags : uint
{
    None = 0,
    Frameless =  1 << 0,
    Floating =  1 << 1,
    Resizable =  1 << 2,
    Visible =  1 << 3,
    Transparent =  1 << 4,
    Focused =  1 << 5,
}