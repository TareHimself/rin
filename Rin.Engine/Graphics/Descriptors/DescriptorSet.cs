﻿using System.Diagnostics;
using System.Runtime.InteropServices;
using Rin.Engine.Graphics.Shaders;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Engine.Graphics.Descriptors;

public class DescriptorSet
{
    private readonly VkDescriptorSet _descriptorSet;
    private readonly VkDevice _device;

    public DescriptorSet(VkDevice device, VkDescriptorSet descriptorSet)
    {
        _device = device;
        _descriptorSet = descriptorSet;
    }

    private readonly List<VkDescriptorImageInfo> _pendingImages = [];
    private readonly List<VkDescriptorBufferInfo> _pendingBuffers = [];
    private readonly List<int> _infoIndex = [];
    private readonly List<VkWriteDescriptorSet> _pendingWrite = [];
    public DescriptorSet WriteSampler(uint binding,in SamplerSpec spec, uint arrayOffset = 0)
    {
        var info = new VkDescriptorImageInfo
        {
            sampler = SGraphicsModule.Get().GetSampler(spec),
        };
        
        var write = new VkWriteDescriptorSet
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
            descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_SAMPLER,
            dstBinding = binding,
            pImageInfo = null,
            descriptorCount = 1,
            dstSet = _descriptorSet,
            dstArrayElement = arrayOffset
        };
        _infoIndex.Add(_pendingImages.Count);
        _pendingImages.Add(info);
        _pendingWrite.Add(write);
        return this;
    }
    public DescriptorSet WriteSampledImage(uint binding, IDeviceImage image, ImageLayout layout, uint arrayOffset = 0)
    {
        var info = new VkDescriptorImageInfo
        { 
            imageView = image.NativeView,
            imageLayout = layout.ToVk()
        };
        
        var write = new VkWriteDescriptorSet
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
            descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_SAMPLED_IMAGE,
            dstBinding = binding,
            pImageInfo = null,
            descriptorCount = 1,
            dstSet = _descriptorSet,
            dstArrayElement = arrayOffset
        };
        _infoIndex.Add(_pendingImages.Count);
        _pendingImages.Add(info);
        _pendingWrite.Add(write);
        return this;
    }
    
    public DescriptorSet WriteSampledImageCombined(uint binding, IDeviceImage image, ImageLayout layout,in SamplerSpec samplerSpec, uint arrayOffset = 0)
    {
        var info = new VkDescriptorImageInfo
        { 
            imageView = image.NativeView,
            imageLayout = layout.ToVk(),
            sampler = SGraphicsModule.Get().GetSampler(samplerSpec),
        };
        
        var write = new VkWriteDescriptorSet
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
            descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
            dstBinding = binding,
            pImageInfo = null,
            descriptorCount = 1,
            dstSet = _descriptorSet,
            dstArrayElement = arrayOffset
        };
        _infoIndex.Add(_pendingImages.Count);
        _pendingImages.Add(info);
        _pendingWrite.Add(write);
        return this;
    }
    public DescriptorSet WriteStorageImage(uint binding, IDeviceImage image, ImageLayout layout, uint arrayOffset = 0)
    {
        var info = new VkDescriptorImageInfo
        { 
            imageView = image.NativeView,
            imageLayout = layout.ToVk()
        };
        
        var write = new VkWriteDescriptorSet
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
            descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_IMAGE,
            dstBinding = binding,
            pImageInfo = null,
            descriptorCount = 1,
            dstSet = _descriptorSet,
            dstArrayElement = arrayOffset
        };
        _infoIndex.Add(_pendingImages.Count);
        _pendingImages.Add(info);
        _pendingWrite.Add(write);
        return this;
    }
    public DescriptorSet WriteStorageBuffer(uint binding, in DeviceBufferView buffer,uint arrayOffset = 0)
    {
        Debug.Assert(buffer.IsValid);
        
        var info = new VkDescriptorBufferInfo()
        { 
            buffer = buffer.Buffer.NativeBuffer,
            offset = buffer.Offset,
            range = buffer.Size
        };
        
        var write = new VkWriteDescriptorSet
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
            descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_BUFFER,
            dstBinding = binding,
            pImageInfo = null,
            descriptorCount = 1,
            dstSet = _descriptorSet,
            dstArrayElement = arrayOffset
        };
        _infoIndex.Add(_pendingImages.Count);
        _pendingBuffers.Add(info);
        _pendingWrite.Add(write);
        return this;
    }
    public DescriptorSet WriteUniformBuffer(uint binding, in DeviceBufferView buffer,uint arrayOffset = 0)
    {
        Debug.Assert(buffer.IsValid);
        
        var info = new VkDescriptorBufferInfo()
        { 
            buffer = buffer.Buffer.NativeBuffer,
            offset = buffer.Offset,
            range = buffer.Size
        };
        
        var write = new VkWriteDescriptorSet
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
            descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER,
            dstBinding = binding,
            pImageInfo = null,
            descriptorCount = 1,
            dstSet = _descriptorSet,
            dstArrayElement = arrayOffset
        };
        _infoIndex.Add(_pendingImages.Count);
        _pendingBuffers.Add(info);
        _pendingWrite.Add(write);
        return this;
    }
   
    public DescriptorSet Update()
    {
        if(_pendingWrite.Count == 0) return this;
        unsafe
        {
            fixed (VkDescriptorBufferInfo* pBuffers = CollectionsMarshal.AsSpan(_pendingBuffers))
            fixed (VkDescriptorImageInfo* pImages = CollectionsMarshal.AsSpan(_pendingImages))
            fixed (VkWriteDescriptorSet* pWrites = CollectionsMarshal.AsSpan(_pendingWrite))
            {
                for (var i = 0; i < _pendingWrite.Count; i++)
                {
                    var write = pWrites + i;
                    var infoIdx = _infoIndex[i];
                    switch (write->descriptorType)
                    {
                        case VkDescriptorType.VK_DESCRIPTOR_TYPE_SAMPLER:
                        case VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER:
                        case VkDescriptorType.VK_DESCRIPTOR_TYPE_SAMPLED_IMAGE:
                        case VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_IMAGE:
                            write->pImageInfo = pImages + infoIdx;
                            break;
                        case VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER:
                        case VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_BUFFER:
                        case VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER_DYNAMIC:
                        case VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_BUFFER_DYNAMIC:
                            write->pBufferInfo = pBuffers + infoIdx;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                vkUpdateDescriptorSets(_device,(uint)_pendingWrite.Count,pWrites,0,null);
            }
            _pendingBuffers.Clear();
            _pendingImages.Clear();
            _infoIndex.Clear();
            _pendingWrite.Clear();
        }

        return this;
    }

    public static explicit operator VkDescriptorSet(DescriptorSet set)
    {
        return set._descriptorSet;
    }
}