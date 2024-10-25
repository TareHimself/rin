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

    private bool SetResource(uint binding, IEnumerable<MultiDisposable> items)
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

        return true;
    }
    
    private bool SetResource(uint binding, IEnumerable<Pair<MultiDisposable,string>> items)
    {
        if (_resources.TryGetValue(binding, out var resource))
        {
            resource.Dispose();
            _resources.Remove(binding);
        }


        _resources.Add(binding, new Resource(items.Select(item =>
        {
            item.First.Reserve();
            
            return new Pair<IAeroxDisposable,string>(item.First,item.Second);
        })));

        return true;
    }

    public bool WriteImage(uint binding, DeviceImage image, VkImageLayout layout, ImageType type, VkSampler sampler)
    {
        if (!SetResource(binding, [image])) return false;
        
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
            
        }

        return true;
    }

    public bool WriteImages(uint binding, DeviceImage[] images, VkImageLayout layout, ImageType type,
        VkSampler sampler)
    {
        if (!SetResource(binding, images)) return false;
        
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
            }
        }

        return true;
    }

    public bool WriteBuffer(uint binding, DeviceBuffer buffer, BufferType type, ulong offset = 0,ulong? size = null)
    {
        if (!SetResource(binding, [new Pair<MultiDisposable, string>(buffer,offset.ToString())])) return false;
        var descriptorInfo = new VkDescriptorBufferInfo
        {
            buffer = buffer.Buffer,
            offset = offset,
            range = size ?? buffer.Size
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
        }

        return true;
    }

    public bool WriteBuffers(uint binding, Pair<DeviceBuffer, ulong?>[] buffers, BufferType type)
    {
        if (!SetResource(binding, buffers.Select(b => new Pair<MultiDisposable, string>(b.First,b.Second.ToString() ?? "0")))) return false;
        
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
            }
        }

        return true;
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
        public string ResourceId { get; private set; }
        private readonly IAeroxDisposable[] _resources;

        public Resource(IEnumerable<IAeroxDisposable> resources)
        {
            _resources = resources.ToArray();
            ResourceId = _resources.Aggregate("", (t, c) => t + c.DisposeId);
        }
        
        public Resource(IEnumerable<Pair<IAeroxDisposable,string>> resources)
        {
            var resourcesArray = resources.ToArray();
            _resources = resourcesArray.Select(c => c.First).ToArray();
            ResourceId = resourcesArray.Aggregate("", (t, c) => t + c.First.DisposeId + c.Second);
        }

        protected override void OnDispose(bool isManual)
        {
            foreach (var resource in _resources) resource.Dispose();
        }
    }
    
    private class BufferResource : IAeroxDisposable
    {
        private DeviceBuffer _buffer;

        public BufferResource(DeviceBuffer buffer,uint offset = 0)
        {
            _buffer = buffer;
        }

        public void Dispose()
        {
            _buffer.Dispose();
        }

        public bool Disposed { get; set; }
        public string DisposeId { get; }
    }
}