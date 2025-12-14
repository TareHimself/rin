using Rin.Framework.Graphics.Windows;
using Rin.Framework.Views.Graphics;

namespace Rin.Framework.Views.Events;

public class CharacterSurfaceEvent(ISurface surface, char character, InputModifier mods) : SurfaceEvent(surface)
{
    public char Character = character;
    public InputModifier Mods = mods;
}