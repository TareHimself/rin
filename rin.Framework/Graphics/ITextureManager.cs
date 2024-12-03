namespace rin.Framework.Graphics;

public interface ITextureManager : IDisposable
{
    public DescriptorSet GetDescriptorSet() => _descriptorSet;

    public async Task<int> CreateTexture(NativeBuffer<byte> data, VkExtent3D size, ImageFormat format,
        ImageFilter filter = ImageFilter.Linear,
        ImageTiling tiling = ImageTiling.Repeat, bool mipMapped = false, string debugName = "Texture")
    {
        
        var image = await SGraphicsModule.Get().CreateImage(data, size, format,
            VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT, mipMapped, filter, debugName);

        var boundText = new BoundTexture(image, filter, tiling, mipMapped, debugName);

        
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
            _textures[textureId] = new BoundTexture();
            _availableIndices.Add(textureId);
        }
        // VkDescriptorSet set = _descriptorSet;
        //
        // List<VkWriteDescriptorSet> writes = [];
        //
        // foreach (var textureId in textureIds)
        // {
        //     var info = _textures[textureId];
        //
        //     if (!info.Valid) continue;
        //
        //     unsafe
        //     {
        //         var imageInfo = new VkDescriptorImageInfo()
        //         {
        //         };
        //
        //         unsafe
        //         {
        //             writes.Add(new VkWriteDescriptorSet()
        //             {
        //                 sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
        //                 dstSet = set,
        //                 dstBinding = 0,
        //                 dstArrayElement = (uint)textureId,
        //                 descriptorCount = 1,
        //                 descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
        //                 pImageInfo = &imageInfo
        //             });
        //         }
        //     }
        // }
        //
        // if (writes.Count != 0)
        // {
        //     unsafe
        //     {
        //         fixed (VkWriteDescriptorSet* writeSets = writes.ToArray())
        //         {
        //             vkUpdateDescriptorSets(SGraphicsModule.Get().GetDevice(), (uint)writes.Count, writeSets, 0, null);
        //         }
        //     }
        // }
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

    protected override void OnDispose(bool isManual)
    {
        //FreeTextures(_textures.Select((_, i) => i).ToArray());
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


    public ITexture? GetTexture(int textureId);

    public IDeviceImage? GetTextureImage(int textureId);

    public bool IsTextureIdValid(int textureId);
}