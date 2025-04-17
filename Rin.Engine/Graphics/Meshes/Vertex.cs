using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Math;

namespace Rin.Engine.Graphics.Meshes;

public struct Vertex : IVertex
{
    /// <summary>
    ///    Location (XYZ) U (W)
    /// </summary>
    private Vector4 _locationU = Vector4.Zero;

    /// <summary>
    ///    Normal (XYZ) V (W)
    /// </summary>
    private Vector4 _normalV = Vector4.Zero;

    /// <summary>
    ///    NOT USED RIGHT NOW
    /// </summary>
    private Vector4 _tangent = Vector4.Zero;

    [PublicAPI]
    public Vector3 Location
    {
        get => new(_locationU.X, _locationU.Y, _locationU.Z);
        set
        {
            _locationU.X = value.X;
            _locationU.Y = value.Y;
            _locationU.Z = value.Z;
        }
    }

    [PublicAPI]
    public Vector3 Normal
    {
        get => new(_normalV.X, _normalV.Y, _normalV.Z);
        set
        {
            _normalV.X = value.X;
            _normalV.Y = value.Y;
            _normalV.Z = value.Z;
        }
    }

    [PublicAPI]
    public Vector3 Tangent
    {
        get => new(_tangent.X, _tangent.Y, _tangent.Z);
        set
        {
            _tangent.X = value.X;
            _tangent.Y = value.Y;
            _tangent.Z = value.Z;
        }
    }

    [PublicAPI]
    public Vector2 UV
    {
        get => new(_locationU.W, _normalV.W);
        set
        {
            _locationU.W = value.X;
            _normalV.W = value.Y;
        }
    }

    public Vertex()
    {
    }

    public Vertex(Vector3 location, Vector3 normal, Vector2 uv)
    {
        Location = location;
        Normal = normal;
        UV = uv;
    }
}