using System.Numerics;

namespace Rin.Engine.Graphics.Meshes;

public struct Vertex
{
    /// <summary>
    /// Location (XYZ) U (W)
    /// </summary>
    private Vector4 _location = Vector4.Zero;

    /// <summary>
    /// Normal (XYZ) V (W)
    /// </summary>
    private Vector4 _normal = Vector4.Zero;

    public Vector3 Location
    {
        get => new Vector3(_location.X, _location.Y, _location.Z);
        set
        {
            _location.X = value.X;
            _location.Y = value.Y;
            _location.Z = value.Z;
        }
    }

    public Vector3 Normal
    {
        get => new Vector3(_normal.X, _normal.Y, _normal.Z);
        set
        {
            _normal.X = value.X;
            _normal.Y = value.Y;
            _normal.Z = value.Z;
        }
    }

    public Vector2 UV
    {
        get => new Vector2(_location.W, _normal.W);
        set
        {
            _location.W = value.X;
            _normal.W = value.Y;
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