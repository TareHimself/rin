using Rin.Framework.Graphics.Meshes;

namespace Rin.Engine.World.Graphics;

public interface IMeshCommand : ICommand
{
    public bool CastShadow { get; set; }

    public int[] SurfaceIndices { get; init; }
    public IMeshMaterial[] Materials { get; init; }
    public IMesh Mesh { get; init; }
}