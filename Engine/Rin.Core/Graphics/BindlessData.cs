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
    public const int FilterCount = 2;
    public const int TilingCount = 3;
    public const int SamplerCount = FilterCount * TilingCount;
    public const int TextureCount = 2048;
    public const int TextureArrayCount = 512;
    public const int CubemapCount = 512;

    public BindlessSamplers Samplers;
    public BindlessTextures Textures;
    public BindlessTextureArrays TextureArrays;
    public BindlessCubemaps Cubemaps;

    public SamplerState GetSampler(ImageTiling tiling, ImageFilter filter)
    {
        return Samplers[(int)filter * TilingCount + (int)tiling];
    }

    public Vector2 GetTextureSize(DeviceHandle handle)
    {
        Textures[(int)Shader.NonUniformResourceIndex(handle.Id)].GetDimensions(0u, out var width, out var height, out var levels);
        return new Vector2(width, height);
    }

    public Vector4 TexelLoad(DeviceHandle handle, int x, int y)
    {
        return Textures[(int)Shader.NonUniformResourceIndex(handle.Id)].Load(x, y, 0);
    }

    public Vector4 SampleTexture(DeviceHandle handle, Vector2 uv, ImageTiling tiling, ImageFilter filter)
    {
        var sampler = GetSampler(tiling, filter);
        return Textures[(int)Shader.NonUniformResourceIndex(handle.Id)].Sample(sampler, uv);
    }
}
