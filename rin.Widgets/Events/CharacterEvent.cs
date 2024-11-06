using rin.Widgets.Graphics;

namespace rin.Widgets.Events;

public class CharacterEvent(Surface surface, char character) : Event(surface)
{
    public char Character = character;
}