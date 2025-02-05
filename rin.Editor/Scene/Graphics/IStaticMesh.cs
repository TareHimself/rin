using rin.Framework.Core;
using rin.Framework.Graphics.Shaders;

namespace rin.Editor.Scene.Graphics;

public interface IStaticMesh : IReservable
{
    public MeshSurface[] Surfaces { get; }
    public DeviceGeometry Geometry { get; }
    public IMeshMaterial[] Materials { get; }
}