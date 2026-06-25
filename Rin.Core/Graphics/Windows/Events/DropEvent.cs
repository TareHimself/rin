namespace Rin.Core.Graphics.Windows.Events;

public class DropEvent : WindowEvent
{
    public required string[] Paths;
    public required string[] Texts;
}