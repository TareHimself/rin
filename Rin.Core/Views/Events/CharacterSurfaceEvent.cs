using Rin.Core.Graphics.Windows;
using Rin.Core.Views.Graphics;

namespace Rin.Core.Views.Events;

public class CharacterSurfaceEvent(ISurface surface, char character, InputModifier mods) : SurfaceEvent(surface)
{
    public char Character = character;
    public InputModifier Mods = mods;
}