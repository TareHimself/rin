using rin.Framework.Graphics.Windows;
using rin.Framework.Widgets.Graphics;

namespace rin.Framework.Widgets.Events;

public class CharacterEvent(Surface surface, char character,InputModifier mods) : Event(surface)
{
    public char Character = character;
    public InputModifier Mods = mods;
}