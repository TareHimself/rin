using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Shade;
using Rin.World.Graphics.Surfaces;
using static Rin.Shade.Shader;

namespace Examples.SceneTest;

/// <summary>
///     Leans an upright mesh along X over time, anchored at <see cref="BaseY" />, to show
///     <see cref="ISurfaceMaterial{TSelf}.ProcessVertex" /> and a material with its own depth shader.
/// </summary>
[NoReorder]
public struct WavyMaterial : ISurfaceMaterial<WavyMaterial>
{
    public Vector3 Color;

    /// <summary>
    ///     At most <see cref="MaxVertexOffset" />.
    /// </summary>
    public float Amplitude;

    public float Speed;

    /// <summary>
    ///     World height of the mesh's base, which stays put, and how far above it the full lean is reached.
    /// </summary>
    public float BaseY;

    public float Height;

    public static IGraphicsDescriptor Shader => WavyShader.Descriptor;
    public static IGraphicsDescriptor? DepthShader => WavyDepthShader.Descriptor;
    public static float MaxVertexOffset => 2f;

    public static WorldVertex ProcessVertex(in WavyMaterial material, in WorldVertex vertex, float time)
    {
        var moved = vertex;
        var lean = Saturate((vertex.Position.Y - material.BaseY) / material.Height);
        moved.Position.X += Sin(time * material.Speed) * material.Amplitude * lean;
        return moved;
    }

    public static Surface Evaluate(in WavyMaterial material, in SurfaceInput input)
    {
        Surface surface;
        surface.BaseColor = material.Color;
        surface.Normal = input.WorldNormal;
        surface.Roughness = 0.4f;
        surface.Metallic = 0f;
        surface.Specular = 0.5f;
        surface.Emissive = 0f;
        return surface;
    }
}

[Shader]
public partial class WavyShader : SurfaceShader<WavyMaterial>;

[Shader]
public partial class WavyDepthShader : SurfaceDepthShader<WavyMaterial>;
