using Rin.Engine.Graphics.Windows;
using Rin.Engine.Views.Graphics;

namespace Rin.Engine.Views.Events;

public class CharacterEvent(Surface surface, char character, InputModifier mods) : Event(surface)
{
    public char Character = character;
    public InputModifier Mods = mods;
}