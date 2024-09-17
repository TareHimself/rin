#define VMA_IMPLEMENTATION
#include "aerox/graphics/Allocator.hpp"
#include "aerox/graphics/GraphicsModule.hpp"
#include "aerox/graphics/DeviceBuffer.hpp"
#include "aerox/graphics/DeviceImage.hpp"

namespace aerox::graphics
{
    Allocator::Allocator(const GraphicsModule  * module)
{

    auto allocatorCreateInfo = VmaAllocatorCreateInfo{};
    allocatorCreateInfo.flags = VMA_ALLOCATOR_CREATE_BUFFER_DEVICE_ADDRESS_BIT;
    allocatorCreateInfo.device = module->GetDevice();
    allocatorCreateInfo.physicalDevice = module->GetPhysicalDevice();
    allocatorCreateInfo.instance = module->GetInstance();
    allocatorCreateInfo.vulkanApiVersion = VK_MAKE_VERSION(1,3,0);

    vmaCreateAllocator(&allocatorCreateInfo, &_allocator);
}

Allocator::~Allocator()
{
    vmaDestroyAllocator(_allocator);
}

Shared<DeviceBuffer> Allocator::NewBuffer(vk::DeviceSize size, vk::BufferUsageFlags usageFlags,
                                          vk::MemoryPropertyFlags propertyFlags, bool sequentialWrite, bool preferHost, bool mapped,
                                          const std::string& debugName)
{

    VmaAllocationCreateFlags createFlags = sequentialWrite
                                               ? VMA_ALLOCATION_CREATE_HOST_ACCESS_SEQUENTIAL_WRITE_BIT
                                               : VMA_ALLOCATION_CREATE_HOST_ACCESS_RANDOM_BIT;
    if (mapped)
    {
        createFlags |= VMA_ALLOCATION_CREATE_MAPPED_BIT;
    }

    const auto bufferInfo = vk::BufferCreateInfo({}, size,usageFlags);
    //vma::AllocationCreateFlagBits::eMapped
    VmaAllocationCreateInfo vmaAllocInfo = {};
    vmaAllocInfo.flags = createFlags;
    vmaAllocInfo.usage = preferHost ? VMA_MEMORY_USAGE_AUTO_PREFER_HOST : VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE;
    vmaAllocInfo.requiredFlags = static_cast<VkMemoryPropertyFlags>(propertyFlags);


    const VkBufferCreateInfo vmaBufferCreateInfo = bufferInfo;
    VkBuffer rawBuffer{};
    VmaAllocation alloc{};
    vmaCreateBuffer(_allocator, &vmaBufferCreateInfo, &vmaAllocInfo,&rawBuffer,&alloc,
                    nullptr);
    vmaSetAllocationName(_allocator,alloc,debugName.c_str());

    return newShared<DeviceBuffer>(_allocator,rawBuffer,alloc,size);
}

Shared<DeviceBuffer> Allocator::NewTransferBuffer(const vk::DeviceSize size, const bool sequentialWrite, const std::string& debugName)
{
    return NewBuffer(size,vk::BufferUsageFlagBits::eTransferSrc | vk::BufferUsageFlagBits::eTransferDst,vk::MemoryPropertyFlagBits::eHostVisible | vk::MemoryPropertyFlagBits::eHostCoherent,sequentialWrite,true,true,debugName);
}

Shared<DeviceBuffer> Allocator::NewUniformBuffer(vk::DeviceSize size, bool sequentialWrite, const std::string& debugName)
{
    return NewBuffer(size,vk::BufferUsageFlagBits::eUniformBuffer,vk::MemoryPropertyFlagBits::eHostVisible,sequentialWrite,false,true,debugName);
}

Shared<DeviceBuffer> Allocator::NewStorageBuffer(vk::DeviceSize size, bool sequentialWrite,
    const std::string& debugName)
{
        return NewBuffer(size,vk::BufferUsageFlagBits::eStorageBuffer,vk::MemoryPropertyFlagBits::eHostVisible,sequentialWrite,false,true,debugName);
}

Shared<DeviceBuffer> Allocator::NewResourceBuffer(const ShaderResource& resource, bool sequentialWrite,
    const std::string& debugName)
{
        auto isStorageBuffer = resource.type == vk::DescriptorType::eStorageBuffer;
        auto hasDebug = debugName.empty();
        if(isStorageBuffer)
        {
            if(hasDebug)
            {
                return NewStorageBuffer(resource.size,sequentialWrite,debugName);
            }
            return NewStorageBuffer(resource.size,sequentialWrite,debugName);
        }

        if(hasDebug)
        {
            return NewUniformBuffer(resource.size,sequentialWrite,debugName);
        }
        return NewUniformBuffer(resource.size,sequentialWrite,debugName);
}

Shared<DeviceImage> Allocator::NewImage(const vk::ImageCreateInfo& createInfo, const std::string& debugName)
{
    VmaAllocationCreateInfo imageAllocInfo = {};

    imageAllocInfo.usage = VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE;
    imageAllocInfo.requiredFlags = VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT;
    const auto vmaImageCreateInfo = static_cast<VkImageCreateInfo>(createInfo);

    VkImage vmaImage;
    VmaAllocation alloc;
    auto result = vmaCreateImage(_allocator,&vmaImageCreateInfo, &imageAllocInfo, &vmaImage, &alloc,
                                 nullptr);

    if (result != VK_SUCCESS)
    {
        throw std::runtime_error("Failed to create image");
    }

    vmaSetAllocationName(_allocator, alloc, debugName.c_str());

    return  newShared<DeviceImage>(_allocator,vmaImage,alloc,createInfo.extent,createInfo.format);
}

}

