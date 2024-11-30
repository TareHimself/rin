using System.Runtime.InteropServices;
using rin.Core;
using rin.Graphics;
using rin.Graphics.Material;
using rin.Core.Math;

namespace rin.Scene.Graphics;

public class StaticMesh : Reservable
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