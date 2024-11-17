using rin.Widgets.Graphics;
using rin.Windows;
using rin.Windows.Native.src;

namespace rin.Widgets.Events;

public class CharacterEvent(Surface surface, char character,InputModifier mods) : Event(surface)
{
    public char Character = character;
    public InputModifier Mods = mods;
}