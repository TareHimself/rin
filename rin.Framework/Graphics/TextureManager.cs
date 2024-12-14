using rin.Framework.Core;
using rin.Framework.Graphics.Descriptors;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace rin.Framework.Graphics;

public class TextureManager : ITextureManager
{
    private static readonly uint MAX_TEXTURES = 512;
    private readonly Mutex _mutex = new();

    private readonly DescriptorAllocator _allocator = new DescriptorAllocator(512, [
        new PoolSizeRatio(VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER, 1.0f)
    ], VkDescriptorPoolCreateFlags.VK_DESCRIPTOR_POOL_CREATE_UPDATE_AFTER_BIND_BIT);

    private readonly HashSet<int> _availableIndices = [];
    private readonly List<Texture> _textures = [];
    private readonly DescriptorSet _descriptorSet;

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
                MAX_TEXTURES,
                flags
            ).Build();

            _descriptorSet = _allocator.Allocate(layout, MAX_TEXTURES);
        }
    }

    public DescriptorSet GetDescriptorSet() => _descriptorSet;

    public async Task<int> CreateTexture(NativeBuffer<byte> data, VkExtent3D size, ImageFormat format,
        ImageFilter filter = ImageFilter.Linear,
        ImageTiling tiling = ImageTiling.Repeat, bool mipMapped = false, string debugName = "Texture")
    {
        
        var image = await SGraphicsModule.Get().CreateImage(data, size, format,
            VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT, mipMapped, filter, debugName);

        var boundText = new Texture(0,image, filter, tiling, mipMapped, debugName);

        
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
        foreach (var textureId in textureIds)
        {
            var info = _textures[textureId];

            if (!info.Valid) continue;

            info.Image?.Dispose();
            _textures[textureId] = new Texture();
            _availableIndices.Add(textureId);
        }
    }

    public ITexture? GetTexture(int textureId)
    {
        return IsTextureIdValid(textureId) ? _textures[textureId] : null;
    }

    private void UpdateTextures(params int[] textureIds)
    {
        VkDescriptorSet set = _descriptorSet;

        List<VkWriteDescriptorSet> writes = [];

        foreach (var textureId in textureIds)
        {
            var info = _textures[textureId];

            if (!info.Valid) continue;

            var sampler = SGraphicsModule.Get().GetSampler(new SamplerSpec()
            {
                Filter = info.Filter,
                Tiling = info.Tiling
            });

            var imageInfo = new VkDescriptorImageInfo()
            {
                sampler = sampler,
                imageView = info.Image!.NativeView,
                imageLayout = VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL
            };

            unsafe
            {
                writes.Add(new VkWriteDescriptorSet()
                {
                    sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
                    dstSet = set,
                    dstBinding = 0,
                    dstArrayElement = (uint)textureId,
                    descriptorCount = 1,
                    descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
                    pImageInfo = &imageInfo
                });
            }
        }

        if (writes.Count != 0)
        {
            unsafe
            {
                fixed (VkWriteDescriptorSet* writeSets = writes.ToArray())
                {
                    vkUpdateDescriptorSets(SGraphicsModule.Get().GetDevice(), (uint)writes.Count, writeSets, 0, null);
                }
            }
        }
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

    public void Dispose()
    {
        _allocator.Dispose();
        foreach (var boundTexture in _textures)
        {
            if (boundTexture.Valid)
            {
                boundTexture.Image!.Dispose();
            }
        }

        _textures.Clear();
    }
}