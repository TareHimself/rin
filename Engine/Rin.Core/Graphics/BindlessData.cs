using System.Numerics;
using System.Runtime.CompilerServices;
using Rin.Shade;

namespace Rin.Core.Graphics;

[InlineArray(BindlessData.SamplerCount)]
public struct BindlessSamplers
{
    private SamplerState _element;
}

[InlineArray(BindlessData.TextureCount)]
public struct BindlessTextures
{
    private Texture2D _element;
}

[InlineArray(BindlessData.TextureArrayCount)]
public struct BindlessTextureArrays
{
    private Texture2DArray _element;
}

[InlineArray(BindlessData.CubemapCount)]
public struct BindlessCubemaps
{
    private TextureCube _element;
}

[ShadeExport]
[BindlessBlock(Name)]
public struct BindlessData
{
    public const string Name = "rin.global";
    public const int SamplerCount = 6;
    public const int TextureCount = 2048;
    public const int TextureArrayCount = 512;
    public const int CubemapCount = 512;

    public BindlessSamplers Samplers;
    public BindlessTextures Textures;
    public BindlessTextureArrays TextureArrays;
    public BindlessCubemaps Cubemaps;

    public SamplerState GetSampler(ImageTiling tiling, ImageFilter filter)
    {
        return Samplers[(int)filter * 2 + (int)tiling];
    }

    public Vector2 GetTextureSize(DeviceHandle handle)
    {
        uint width;
        uint height;
        uint levels;
        Textures[(int)Shader.NonUniformResourceIndex(handle.Id)].GetDimensions(0u, out width, out height, out levels);
        return new Vector2(width, height);
    }

    public Vector4 SampleTexture(DeviceHandle handle, Vector2 uv, ImageTiling tiling, ImageFilter filter)
    {
        var sampler = GetSampler(tiling, filter);
        return Textures[(int)Shader.NonUniformResourceIndex(handle.Id)].Sample(sampler, uv);
    }
}
