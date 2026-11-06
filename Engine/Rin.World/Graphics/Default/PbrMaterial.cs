using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Shade;
using Rin.World.Graphics.Surfaces;

namespace Rin.World.Graphics.Default;

/// <summary>
///     The engine's default metallic-roughness material. Each channel samples its texture when one is set and falls
///     back to the constant otherwise.
/// </summary>
[NoReorder]
public struct PbrMaterial : ISurfaceMaterial<PbrMaterial>
{
    public Vector3 BaseColor;
    public DeviceHandle BaseColorTexture;
    public float Metallic;
    public DeviceHandle MetallicTexture;
    public float Specular;
    public DeviceHandle SpecularTexture;
    public float Roughness;
    public DeviceHandle RoughnessTexture;
    public float Emissive;
    public DeviceHandle EmissiveTexture;

    public PbrMaterial()
    {
        BaseColor = new Vector3(1f);
    }

    public static IGraphicsDescriptor Shader => PbrShader.Descriptor;

    public static Surface Evaluate(in PbrMaterial material, in SurfaceInput input, BindlessData textures)
    {
        Surface surface;
        surface.BaseColor = textures.ColorOr(material.BaseColorTexture, input.Uv, material.BaseColor);
        surface.Normal = input.WorldNormal;
        surface.Roughness = textures.ValueOr(material.RoughnessTexture, input.Uv, material.Roughness);
        surface.Metallic = textures.ValueOr(material.MetallicTexture, input.Uv, material.Metallic);
        surface.Specular = textures.ValueOr(material.SpecularTexture, input.Uv, material.Specular);
        surface.Emissive = textures.ValueOr(material.EmissiveTexture, input.Uv, material.Emissive);
        return surface;
    }

    public static void GetTextures(in PbrMaterial material, ref TextureList textures)
    {
        textures.Add(material.BaseColorTexture);
        textures.Add(material.MetallicTexture);
        textures.Add(material.SpecularTexture);
        textures.Add(material.RoughnessTexture);
        textures.Add(material.EmissiveTexture);
    }
}

[Shader]
public partial class PbrShader : SurfaceShader<PbrMaterial>;
