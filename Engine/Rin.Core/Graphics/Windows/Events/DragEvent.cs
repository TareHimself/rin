using System.Numerics;

namespace Rin.Core.Graphics.Windows.Events;

/// <summary>
///     Data another app is dragging over the window. Handlers answer by setting <see cref="Effect" />.
/// </summary>
public class DragEvent : WindowEvent
{
    public required Vector2 Position;
    public required string[] Paths;
    public string? Text;
    public required DropEffect Allowed;
    public InputModifier Modifiers;

    /// <summary>
    ///     The effect the window shows or applies. Starts as the first of copy, move and link that is allowed,
    ///     <see cref="DropEffect.None" /> refuses the drop.
    /// </summary>
    public DropEffect Effect;

    public bool HasPaths => Paths.Length > 0;
    public bool HasText => Text is not null;
}
