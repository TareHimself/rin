namespace Rin.Core.Graphics.Windows;

/// <summary>
///     What happens to dragged data when it is dropped. The dragging app allows a set and the window answers with one.
/// </summary>
[Flags]
public enum DropEffect : uint
{
    None = 0,
    Copy = 1,
    Move = 2,
    Link = 4
}
