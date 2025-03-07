using Rin.Engine.Graphics.Windows;
using Rin.Engine.Views.Graphics;

namespace Rin.Engine.Views.Events;

public class CharacterSurfaceEvent(Surface surface, char character, InputModifier mods) : SurfaceEvent(surface)
{
    public char Character = character;
    public InputModifier Mods = mods;
}