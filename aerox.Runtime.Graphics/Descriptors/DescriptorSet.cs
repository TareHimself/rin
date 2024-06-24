using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace aerox.Runtime.Graphics.Descriptors;

public class DescriptorSet : Disposable
{
    public enum BufferType
    {
        Uniform,
        Storage
    }

    public enum ImageType
    {
        Texture,
        Storage
    }

    private readonly VkDescriptorSet _descriptorSet;
    private readonly VkDevice _device;
    private readonly Dictionary<uint, Resource> _resources = new();


    public DescriptorSet(VkDevice device, VkDescriptorSet descriptorSet)
    {
        _device = device;
        _descriptorSet = descriptorSet;
    }

    public static VkDescriptorType BufferTypeToDescriptorType(BufferType type)
    {
        return type switch
        {
            BufferType.Uniform => VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER,
            BufferType.Storage => VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_BUFFER,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static VkDescriptorType ImageTypeToDescriptorType(ImageType type)
    {
        return type switch
        {
            ImageType.Texture => VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
            ImageType.Storage => VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_IMAGE,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    private void SetResource(uint binding, IEnumerable<MultiDisposable> items)
    {
        if (_resources.TryGetValue(binding, out var resource))
        {
            resource.Dispose();
            _resources.Remove(binding);
        }


        _resources.Add(binding, new Resource(items.Select(item =>
        {
            item.Reserve();

            return item;
        })));
    }


    public void WriteTexture(uint binding, Texture texture, VkImageLayout layout)
    {
        var descriptorImageInfo = new VkDescriptorImageInfo
        {
            sampler = new SamplerSpec
            {
                Filter = texture.Filter,
                Tiling = texture.Tiling
            },
            imageView = texture.DeviceImage.View,
            imageLayout = layout
        };
        unsafe
        {
            var write = new VkWriteDescriptorSet
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
                descriptorType = ImageTypeToDescriptorType(ImageType.Texture),
                dstBinding = binding,
                pImageInfo = &descriptorImageInfo,
                descriptorCount = 1,
                dstSet = _descriptorSet
            };

            vkUpdateDescriptorSets(_device, 1, &write, 0, null);
            SetResource(binding, new[] { texture });
        }
    }

    public void WriteTextures(uint binding, Texture[] textures, VkImageLayout layout)
    {
        var infos = textures.Select(texture => new VkDescriptorImageInfo
        {
            sampler = new SamplerSpec
            {
                Filter = texture.Filter,
                Tiling = texture.Tiling
            },
            imageView = texture.DeviceImage.View,
            imageLayout = layout
        }).ToArray();


        unsafe
        {
            fixed (VkDescriptorImageInfo* pInfos = infos)
            {
                var write = new VkWriteDescriptorSet
                {
                    sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
                    descriptorType = ImageTypeToDescriptorType(ImageType.Texture),
                    dstBinding = binding,
                    pImageInfo = pInfos,
                    descriptorCount = (uint)infos.Length,
                    dstSet = _descriptorSet
                };

                vkUpdateDescriptorSets(_device, 1, &write, 0, null);
                SetResource(binding, textures);
            }
        }
    }

    public void WriteImage(uint binding, DeviceImage image, VkImageLayout layout, ImageType type, VkSampler sampler)
    {
        var descriptorImageInfo = new VkDescriptorImageInfo
        {
            sampler = sampler,
            imageView = image.View,
            imageLayout = layout
        };
        unsafe
        {
            var write = new VkWriteDescriptorSet
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
                descriptorType = ImageTypeToDescriptorType(type),
                dstBinding = binding,
                pImageInfo = &descriptorImageInfo,
                descriptorCount = 1,
                dstSet = _descriptorSet
            };

            vkUpdateDescriptorSets(_device, 1, &write, 0, null);
            SetResource(binding, new[] { image });
        }
    }

    public void WriteImages(uint binding, DeviceImage[] images, VkImageLayout layout, ImageType type,
        VkSampler sampler)
    {
        var infos = images.Select(image => new VkDescriptorImageInfo
        {
            sampler = sampler,
            imageView = image.View,
            imageLayout = layout
        }).ToArray();


        unsafe
        {
            fixed (VkDescriptorImageInfo* pInfos = infos)
            {
                var write = new VkWriteDescriptorSet
                {
                    sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
                    descriptorType = ImageTypeToDescriptorType(type),
                    dstBinding = binding,
                    pImageInfo = pInfos,
                    descriptorCount = (uint)infos.Length,
                    dstSet = _descriptorSet
                };

                vkUpdateDescriptorSets(_device, 1, &write, 0, null);
                SetResource(binding, images);
            }
        }
    }

    public void WriteBuffer(uint binding, DeviceBuffer buffer, BufferType type, ulong offset = 0)
    {
        var descriptorInfo = new VkDescriptorBufferInfo
        {
            buffer = buffer.Buffer,
            offset = 0,
            range = buffer.Size
        };
        unsafe
        {
            var write = new VkWriteDescriptorSet
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
                descriptorType = BufferTypeToDescriptorType(type),
                dstBinding = binding,
                pBufferInfo = &descriptorInfo,
                descriptorCount = 1,
                dstSet = _descriptorSet
            };

            vkUpdateDescriptorSets(_device, 1, &write, 0, null);
            SetResource(binding, new[] { buffer });
        }
    }

    public void WriteBuffers(uint binding, Pair<DeviceBuffer, ulong?>[] buffers, BufferType type)
    {
        var infos = buffers.Select(buffer => new VkDescriptorBufferInfo
        {
            buffer = buffer.First.Buffer,
            offset = buffer.Second ?? 0,
            range = buffer.First.Size
        }).ToArray();


        unsafe
        {
            fixed (VkDescriptorBufferInfo* pInfos = infos)
            {
                var write = new VkWriteDescriptorSet
                {
                    sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
                    descriptorType = BufferTypeToDescriptorType(type),
                    dstBinding = binding,
                    pBufferInfo = pInfos,
                    descriptorCount = (uint)infos.Length,
                    dstSet = _descriptorSet
                };

                vkUpdateDescriptorSets(_device, 1, &write, 0, null);
                SetResource(binding, buffers.Select(b => b.First));
            }
        }
    }

    public static implicit operator VkDescriptorSet(DescriptorSet set)
    {
        return set._descriptorSet;
    }

    protected override void OnDispose(bool isManual)
    {
        foreach (var resource in _resources) resource.Value.Dispose();
        _resources.Clear();
    }

    private class Resource : Disposable
    {
        private readonly IDisposable[] _resources;

        public Resource(IEnumerable<IDisposable> resources)
        {
            _resources = resources.ToArray();
        }

        protected override void OnDispose(bool isManual)
        {
            foreach (var resource in _resources) resource.Dispose();
        }
    }
}