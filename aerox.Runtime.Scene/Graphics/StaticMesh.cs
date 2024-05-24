using System.Runtime.InteropServices;
using aerox.Runtime.Graphics;
using aerox.Runtime.Graphics.Material;
using aerox.Runtime.Math;

namespace aerox.Runtime.Scene.Graphics;

public class StaticMesh : MultiDisposable
{
    public readonly MeshSurface[] Surfaces;
    public MaterialInstance?[] Materials = [];
    public DeviceGeometry? Geometry;
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    // UV in last values of each i.e. U = Location.W, V = Normal.W
    public partial struct Vertex
    {
        public Vector4<float> Location;
        public Vector4<float> Normal;
    }
    

    public StaticMesh(Vertex[] inVertices, uint[] inIndices, MeshSurface[] inSurfaces)
    {
        Surfaces = inSurfaces;
        Geometry = GraphicsModule.Get().NewGeometry(inVertices, inIndices);
    }

    protected override void OnDispose(bool isManual)
    {
        Geometry?.Dispose();
    }
}