using rin.Framework.Graphics.Windows;
using rin.Framework.Views.Graphics;

namespace rin.Framework.Views.Events;

public class CharacterEvent(Surface surface, char character, InputModifier mods) : Event(surface)
{
    public char Character = character;
    public InputModifier Mods = mods;
}