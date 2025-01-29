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
    public IMaterial?[] Materials { get; set; } = [];


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
        var geometry = await SGraphicsModule.Get().NewGeometry(inVertices.ToArray(), inIndices.ToArray());
        return new StaticMesh
        {
            Geometry = geometry,
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

    // UV in last values of each i.e. U = Location.W, V = Normal.W
    
    public struct Vertex
    {
        /// <summary>
        /// Location (XYZ) U (W)
        /// </summary>
        public Vec4<float> Location;
        /// <summary>
        /// Normal (XYZ) V (W)
        /// </summary>
        public Vec4<float> Normal;
    }
}