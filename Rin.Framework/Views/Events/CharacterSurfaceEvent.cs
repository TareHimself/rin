using Rin.Framework.Graphics.Windows;
using Rin.Framework.Views.Graphics;

namespace Rin.Framework.Views.Events;

public class CharacterSurfaceEvent(Surface surface, char character, InputModifier mods) : SurfaceEvent(surface)
{
    public char Character = character;
    public InputModifier Mods = mods;
}