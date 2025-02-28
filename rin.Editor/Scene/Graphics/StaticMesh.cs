using System.Numerics;
using System.Runtime.InteropServices;
using Rin.Editor.Scene.Graphics;
using Rin.Engine.Core;
using Rin.Engine.Core.Extensions;
using Rin.Engine.Core.Math;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Shaders;

namespace Rin.Editor.Scene.Graphics;

public class StaticMesh : Reservable, IStaticMesh
{
    public required MeshSurface[] Surfaces { get; init; }
    public required DeviceGeometry Geometry { get; init; }
    public IMeshMaterial?[] Materials { get; set; } = [];


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
        public Vector4 Location;
        /// <summary>
        /// Normal (XYZ) V (W)
        /// </summary>
        public Vector4 Normal;
    }
}