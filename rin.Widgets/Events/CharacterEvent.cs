using rin.Widgets.Graphics;
using rin.Windows;

namespace rin.Widgets.Events;

public class CharacterEvent(Surface surface, char character,Window.Mods mods) : Event(surface)
{
    public char Character = character;
    public Window.Mods Mods = mods;
}