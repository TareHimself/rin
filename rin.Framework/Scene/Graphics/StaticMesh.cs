using System.Runtime.InteropServices;
using rin.Framework.Core;
using rin.Framework.Core.Extensions;
using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using rin.Framework.Graphics.Shaders;

namespace rin.Framework.Scene.Graphics;

public class StaticMesh : Reservable, IStaticMesh
{
    public required MeshSurface[] Surfaces { get; init; }
    public required DeviceGeometry Geometry { get; init; }
    public IShader?[] Shaders { get; set; } = [];


    // private StaticMesh()
    // {
    //     Surfaces = inSurfaces;
    //     Geometry = SGraphicsModule.Get().NewGeometry(inVertices, inIndices).WaitForResult();
    // }
    //
    // public StaticMesh()
    // {
    //     Surfaces = inSurfaces;
    //     Geometry = geometry;
    // }

    public static async Task<StaticMesh> Create(IEnumerable<MeshSurface> inSurfaces,IEnumerable<Vertex> inVertices,IEnumerable<uint> inIndices)
    {
        return new StaticMesh
        {
            Geometry = await SGraphicsModule.Get().NewGeometry(inVertices.ToArray(), inIndices.ToArray()),
            Surfaces = inSurfaces.ToArray(),
        };
    }

    public static StaticMesh Create(IEnumerable<MeshSurface> inSurfaces,DeviceGeometry geometry)
    {
        return new StaticMesh
        {
            Geometry = geometry,
            Surfaces = inSurfaces.ToArray(),
        };
    }

    protected override void OnDispose(bool isManual)
    {
        Geometry.Dispose();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    // UV in last values of each i.e. U = Location.W, V = Normal.W
    public struct Vertex
    {
        public Vector4<float> Location;
        public Vector4<float> Normal;
    }
}