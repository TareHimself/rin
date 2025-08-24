namespace Rin.Framework.Graphics.Windows.Events;

public class DropEvent : WindowEvent
{
    public required string[] Paths;
    public required string[] Texts;
}