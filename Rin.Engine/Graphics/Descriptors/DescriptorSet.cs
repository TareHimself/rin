using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Engine.Graphics.Descriptors;

public class DescriptorSet : Disposable
{
    private readonly VkDescriptorSet _descriptorSet;
    private readonly VkDevice _device;

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

    public static VkDescriptorType ImageTypeToDescriptorType(DescriptorImageType type)
    {
        return type switch
        {
            DescriptorImageType.Sampled => VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
            DescriptorImageType.Storage => VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_IMAGE,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    // private bool SetResource(uint binding, IEnumerable<IReservable> items)
    // {
    //     if (_resources.TryGetValue(binding, out var resource))
    //     {
    //         resource.Dispose();
    //         _resources.Remove(binding);
    //     }
    //
    //
    //     _resources.Add(binding, new Resource(items.Select(item =>
    //     {
    //         item.Reserve();
    //
    //         return item;
    //     })));
    //
    //     return true;
    // }

    // private bool SetResource(uint binding, IEnumerable<Pair<IReservable,string>> items)
    // {
    //     if (_resources.TryGetValue(binding, out var resource))
    //     {
    //         resource.Dispose();
    //         _resources.Remove(binding);
    //     }
    //
    //
    //     _resources.Add(binding, new Resource(items.Select(item =>
    //     {
    //         item.First.Reserve();
    //         
    //         return new Pair<IDisposable,string>(item.First,item.Second);
    //     })));
    //
    //     return true;
    // }

    public bool WriteImages(uint binding, params ImageWrite[] writes)
    {
        unsafe
        {
            var infos = stackalloc VkDescriptorImageInfo[writes.Length];
            var writeSets = stackalloc VkWriteDescriptorSet[writes.Length];
            for (var i = 0; i < writes.Length; i++)
            {
                var write = writes[i];
                var imageInfo = infos + i;
                imageInfo->sampler = SGraphicsModule.Get().GetSampler(write.Sampler);
                imageInfo->imageView = write.Image.NativeView;
                imageInfo->imageLayout = write.Layout.ToVk();
                var writeSet = writeSets + i;
                writeSet->sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET;
                writeSet->descriptorType = ImageTypeToDescriptorType(write.Type);
                writeSet->dstBinding = binding;
                writeSet->pImageInfo = imageInfo;
                writeSet->descriptorCount = 1;
                writeSet->dstSet = _descriptorSet;
                writeSet->dstArrayElement = write.Index;
            }

            vkUpdateDescriptorSets(_device, (uint)writes.Length, writeSets, 0, null);
        }

        return true;
    }

    public bool WriteBuffers(uint binding, params BufferWrite[] writes)
    {
        // if (!SetResource(binding, writes.Select(c => c.View))) return false;
        //
        var infos = writes.Select(write => new VkDescriptorBufferInfo
        {
            buffer = write.Buffer.NativeBuffer,
            offset = write.Offset,
            range = write.Size
        }).ToArray();

        unsafe
        {
            fixed (VkDescriptorBufferInfo* pInfos = infos)
            {
                var write = new VkWriteDescriptorSet
                {
                    sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
                    descriptorType = BufferTypeToDescriptorType(writes.First().Type),
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

    public static explicit operator VkDescriptorSet(DescriptorSet set)
    {
        return set._descriptorSet;
    }

    protected override void OnDispose(bool isManual)
    {
    }

    private class Resource : Disposable
    {
        private readonly IDisposable[] _resources;

        public Resource(IEnumerable<IDisposable> resources)
        {
            _resources = resources.ToArray();
            ResourceId = _resources.Aggregate("", (t, c) => t + c.GetHashCode());
        }

        public Resource(IEnumerable<Pair<IDisposable, string>> resources)
        {
            var resourcesArray = resources.ToArray();
            _resources = resourcesArray.Select(c => c.First).ToArray();
            ResourceId = resourcesArray.Aggregate("", (t, c) => t + c.First.GetHashCode() + c.Second);
        }

        public string ResourceId { get; private set; }

        protected override void OnDispose(bool isManual)
        {
            foreach (var resource in _resources) resource.Dispose();
        }
    }
}