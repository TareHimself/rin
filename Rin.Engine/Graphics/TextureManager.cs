using Rin.Assets;
using Rin.Engine.Core;
using Rin.Engine.Graphics.Descriptors;
using Rin.Engine.Views;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics;

public class TextureManager : ITextureManager
{
    private const uint MaxTextures = 2048;

    private readonly DescriptorAllocator _allocator = new(MaxTextures, [
        new PoolSizeRatio(VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER, 1.0f)
    ], VkDescriptorPoolCreateFlags.VK_DESCRIPTOR_POOL_CREATE_UPDATE_AFTER_BIND_BIT);

    private readonly HashSet<int> _availableIndices = [];
    private readonly DescriptorSet _descriptorSet;
    private readonly Mutex _mutex = new();
    private readonly List<Texture> _textures = [];
    private uint _count;

    // public class BoundTexture : ITexture
    // {
    //     public readonly IDeviceImage? Image;
    //     public readonly ImageFilter Filter;
    //     public readonly ImageTiling Tiling;
    //     public bool MipMapped = false;
    //     public string DebugName;
    //     public bool Valid => Image != null;
    //
    //     public BoundTexture()
    //     {
    //         DebugName = "";
    //     }
    //
    //     public BoundTexture(IDeviceImage image, ImageFilter filter, ImageTiling tiling, bool mipMapped, string debugName)
    //     {
    //         Image = image;
    //         Filter = filter;
    //         Tiling = tiling;
    //         MipMapped = mipMapped;
    //         DebugName = debugName;
    //     }
    // }

    public TextureManager()
    {
        {
            const VkDescriptorBindingFlags flags = VkDescriptorBindingFlags.VK_DESCRIPTOR_BINDING_PARTIALLY_BOUND_BIT |
                                                   VkDescriptorBindingFlags
                                                       .VK_DESCRIPTOR_BINDING_VARIABLE_DESCRIPTOR_COUNT_BIT |
                                                   VkDescriptorBindingFlags.VK_DESCRIPTOR_BINDING_UPDATE_AFTER_BIND_BIT;
            var layout = new DescriptorLayoutBuilder().AddBinding(
                0,
                VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
                VkShaderStageFlags.VK_SHADER_STAGE_ALL,
                MaxTextures,
                flags
            ).Build();

            _descriptorSet = _allocator.Allocate(layout, MaxTextures);
        }

        _textures.Add(new Texture());

        LoadDefaultTexture().ConfigureAwait(false);
    }

    public DescriptorSet GetDescriptorSet()
    {
        return _descriptorSet;
    }


    public async Task<int> CreateTexture(NativeBuffer<byte> data, Extent3D size, ImageFormat format,
        ImageFilter filter = ImageFilter.Linear,
        ImageTiling tiling = ImageTiling.Repeat, bool mipMapped = false, string debugName = "Texture")
    {
        var image = await SGraphicsModule.Get().CreateImage(data, size, format,
            VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT, mipMapped, filter, debugName);

        var boundText = new Texture(0, image, filter, tiling, mipMapped, debugName);

        lock (_mutex)
        {
            int textureId;

            if (_availableIndices.Count == 0)
            {
                _textures.Add(boundText);
                textureId = _textures.Count - 1;
            }
            else
            {
                textureId = _availableIndices.First();
                _availableIndices.Remove(textureId);
                _textures[textureId] = boundText;
            }

            boundText.Id = textureId;
            UpdateTextures(textureId);

            return textureId;
        }
    }

    public void FreeTextures(params int[] textureIds)
    {
        lock (_mutex)
        {
            List<ImageWrite> writes = [];

            var sampler = new SamplerSpec
            {
                Filter = ImageFilter.Linear,
                Tiling = ImageTiling.Repeat
            };
            var defaultTexture = _textures[0].Image;

            foreach (var textureId in textureIds)
            {
                if (textureId == 0) continue;

                var info = _textures[textureId];

                if (!info.Valid) continue;
                _count--;
                info.Image?.Dispose();
                _textures[textureId] = new Texture();
                _availableIndices.Add(textureId);

                if (defaultTexture != null)
                    writes.Add(new ImageWrite(defaultTexture, ImageLayout.ShaderReadOnly, ImageType.Sampled, sampler)
                    {
                        Index = (uint)textureId
                    });
            }

            if (writes.Count != 0) _descriptorSet.WriteImages(0, writes.ToArray());
        }
    }

    public ITexture? GetTexture(int textureId)
    {
        return IsTextureIdValid(textureId) ? _textures[textureId] : null;
    }

    public IDeviceImage? GetTextureImage(int textureId)
    {
        return GetTexture(textureId)?.Image;
    }

    public bool IsTextureIdValid(int textureId)
    {
        if (textureId < 0) return false;

        if (_availableIndices.Contains(textureId)) return false;

        return textureId < _textures.Count;
    }

    public uint GetMaxTextures()
    {
        return MaxTextures;
    }

    public uint GetTexturesCount()
    {
        return _count;
    }

    public void Dispose()
    {
        _allocator.Dispose();
        foreach (var boundTexture in _textures)
            if (boundTexture.Valid)
                boundTexture.Image?.Dispose();

        _textures.Clear();
    }

    private async Task LoadDefaultTexture()
    {
        using var imgData = await Image.LoadAsync<Rgba32>(RinAssets.FileSystem.OpenRead("Textures.default.png"));
        using var buffer = imgData.ToBuffer();
        var tex = _textures[0];
        tex.Image = await SGraphicsModule.Get().CreateImage(
            buffer,
            new Extent3D
            {
                Width = (uint)imgData.Width,
                Height = (uint)imgData.Height
            },
            ImageFormat.RGBA8,
            VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT,
            true
        );

        List<ImageWrite> writes = [];

        var sampler = new SamplerSpec
        {
            Filter = ImageFilter.Linear,
            Tiling = ImageTiling.Repeat
        };

        for (var i = 0; i < MaxTextures; i++)
        {
            var info = i < _textures.Count ? _textures[i] : null;

            if (info?.Valid == true && i != 0) continue;

            writes.Add(new ImageWrite(tex.Image, ImageLayout.ShaderReadOnly, ImageType.Sampled, sampler)
            {
                Index = (uint)i
            });
        }

        if (writes.Count != 0) _descriptorSet.WriteImages(0, writes.ToArray());
    }

    private void UpdateTextures(params int[] textureIds)
    {
        List<ImageWrite> writes = [];

        foreach (var textureId in textureIds)
        {
            var info = _textures[textureId];

            if (!info.Valid || info.Image is null) continue;

            writes.Add(new ImageWrite(info.Image, ImageLayout.ShaderReadOnly, ImageType.Sampled, new SamplerSpec
            {
                Filter = info.Filter,
                Tiling = info.Tiling
            })
            {
                Index = (uint)textureId
            });
        }

        if (writes.Count != 0) _descriptorSet.WriteImages(0, writes.ToArray());
    }
}