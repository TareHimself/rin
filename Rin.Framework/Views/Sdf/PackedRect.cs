using JetBrains.Annotations;

namespace Rin.Framework.Views.Sdf;

[NoReorder]
public struct PackedRect<T>
{
    public int X;
    public int Y;
    public int Width;
    public int Height;
    public T Data;
}