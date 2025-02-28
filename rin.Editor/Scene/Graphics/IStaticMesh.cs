using Rin.Engine.Core;
using Rin.Engine.Graphics.Shaders;

namespace Rin.Editor.Scene.Graphics;

public interface IStaticMesh : IReservable
{
    public MeshSurface[] Surfaces { get; }
    public DeviceGeometry Geometry { get; }
    public IMeshMaterial[] Materials { get; }
}