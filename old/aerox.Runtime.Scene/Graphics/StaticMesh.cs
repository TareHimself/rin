using System.Runtime.InteropServices;
using aerox.Runtime.Graphics;
using aerox.Runtime.Graphics.Material;
using aerox.Runtime.Math;

namespace aerox.Runtime.Scene.Graphics;

public class StaticMesh : MultiDisposable
{
    public readonly MeshSurface[] Surfaces;
    public DeviceGeometry Geometry;
    public MaterialInstance?[] Materials = [];


    public StaticMesh(Vertex[] inVertices, uint[] inIndices, MeshSurface[] inSurfaces)
    {
        Surfaces = inSurfaces;
        Geometry = SGraphicsModule.Get().NewGeometry(inVertices, inIndices);
    }

    protected override void OnDispose(bool isManual)
    {
        Geometry.Dispose();
        foreach (var materialInstance in Materials)
        {
            materialInstance?.Dispose();
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    // UV in last values of each i.e. U = Location.W, V = Normal.W
    public struct Vertex
    {
        public Vector4<float> Location;
        public Vector4<float> Normal;
    }
}