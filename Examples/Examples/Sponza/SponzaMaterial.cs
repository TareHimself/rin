using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Shade;
using Rin.World.Graphics.Surfaces;

namespace Examples.Sponza;

[NoReorder]
public struct SponzaMaterial : ISurfaceMaterial<SponzaMaterial>
{
    public Vector4 Color;
    public DeviceHandle ColorTexture;
    public DeviceHandle NormalTexture;
    public DeviceHandle MetallicRoughnessTexture;

    public SponzaMaterial()
    {
        Color = Vector4.One;
    }

    public static IGraphicsDescriptor Shader => SponzaShader.Descriptor;

    public static Surface Evaluate(in SponzaMaterial material, in SurfaceInput input)
    {
        Surface surface;
        surface.BaseColor = SurfaceSampling.ColorOr(material.ColorTexture, input.Uv, material.Color.XYZ);
        surface.Normal = input.WorldNormal;
        surface.Roughness = 1f;
        surface.Metallic = 0f;
        surface.Specular = 0f;
        surface.Emissive = 0f;

        // glTF metallic-roughness convention: G is roughness, B is metallic.
        if (material.MetallicRoughnessTexture.Id > 0u)
        {
            var metallicRoughness = SurfaceSampling.ColorOr(material.MetallicRoughnessTexture, input.Uv, new Vector3(0f));
            surface.Roughness = metallicRoughness.Y;
            surface.Metallic = metallicRoughness.Z;
        }

        return surface;
    }

    public static void GetTextures(in SponzaMaterial material, ref TextureList textures)
    {
        textures.Add(material.ColorTexture);
        textures.Add(material.NormalTexture);
        textures.Add(material.MetallicRoughnessTexture);
    }
}

[Shader]
public partial class SponzaShader : SurfaceShader<SponzaMaterial>;
