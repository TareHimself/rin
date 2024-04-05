
#define VMA_IMPLEMENTATION
#include "vk_mem_alloc.h"
#include "aerox/Engine.hpp"
#include <aerox/drawing/DrawingSubsystem.hpp>
#include "VkBootstrap.h"
#include <aerox/drawing/Allocator.hpp>

namespace aerox::drawing {

    void VmaAllocated::Write(const void * src, size_t size, size_t offset) const {
        vmaCopyMemoryToAllocation(_allocator,src,alloc,offset,size);
    }

    void VmaAllocated::Read(void * dst, size_t offset) const {
        VmaAllocationInfo info;
        vmaGetAllocationInfo(_allocator,alloc,&info);
        vmaCopyAllocationToMemory(_allocator,alloc,offset,dst,info.size);
    }

    void Allocator::OnInit(DrawingSubsystem *subsystem) {
      TOwnedBy::OnInit(subsystem);
      auto allocatorCreateInfo = VmaAllocatorCreateInfo{};
      allocatorCreateInfo.flags = VMA_ALLOCATOR_CREATE_BUFFER_DEVICE_ADDRESS_BIT;
      allocatorCreateInfo.device = GetOwner()->GetVirtualDevice();
      allocatorCreateInfo.physicalDevice = GetOwner()->GetPhysicalDevice();
      allocatorCreateInfo.instance = GetOwner()->GetVulkanInstance();
      allocatorCreateInfo.vulkanApiVersion = VKB_MAKE_VK_VERSION(0, 1, 3, 0);

      vmaCreateAllocator(&allocatorCreateInfo, &_allocator);
    }

    void Allocator::OnDestroy() {
        vmaDestroyAllocator(_allocator);
        Object::OnDestroy();
    }

    std::shared_ptr<AllocatedBuffer> Allocator::CreateBuffer(const size_t allocSize,
                                                     const vk::BufferUsageFlags usage, const VmaMemoryUsage memoryUsage,
                                                     const vk::MemoryPropertyFlags requiredFlags,
                                                     const VmaAllocationCreateFlags flags,const std::string& name) const {
        const auto bufferInfo = vk::BufferCreateInfo({}, allocSize,
                                                     usage);
        //vma::AllocationCreateFlagBits::eMapped
        VmaAllocationCreateInfo vmaAllocInfo = {};
        vmaAllocInfo.flags = flags;
        vmaAllocInfo.usage = memoryUsage;
        vmaAllocInfo.requiredFlags = static_cast<VkMemoryPropertyFlags>(requiredFlags);


        std::shared_ptr<AllocatedBuffer> result = std::shared_ptr<AllocatedBuffer>(new AllocatedBuffer(_allocator),
                                                                   [this](const AllocatedBuffer *ptr) {
                                                                       DestroyBuffer(*ptr);
                                                                       delete ptr;
                                                                   });

        const VkBufferCreateInfo vmaBufferCreateInfo = bufferInfo;
        VkBuffer rawBuffer = result->buffer;

        vmaCreateBuffer(_allocator, &vmaBufferCreateInfo, &vmaAllocInfo, &rawBuffer, &result->alloc,
                        nullptr);
        result->buffer = rawBuffer;
        result->size = allocSize;
      vmaSetAllocationName(_allocator,result->alloc,name.c_str());

        return result;
    }

    std::shared_ptr<AllocatedBuffer> Allocator::CreateTransferCpuGpuBuffer(
            const size_t size,
            bool randomAccess,const std::string& name) const {

        return CreateBuffer(size,
                            vk::BufferUsageFlagBits::eTransferSrc,
                            VMA_MEMORY_USAGE_AUTO_PREFER_HOST,
                            vk::MemoryPropertyFlagBits::eHostVisible |
                            vk::MemoryPropertyFlagBits::eHostCoherent,
                            VMA_ALLOCATION_CREATE_MAPPED_BIT | (randomAccess
                                                                ? VMA_ALLOCATION_CREATE_HOST_ACCESS_RANDOM_BIT
                                                                : VMA_ALLOCATION_CREATE_HOST_ACCESS_SEQUENTIAL_WRITE_BIT),name);
    }

    std::shared_ptr<AllocatedBuffer> Allocator::CreateUniformCpuGpuBuffer(
            const size_t size,
            bool randomAccess,const std::string& name) const {
        return CreateBuffer(
                size, vk::BufferUsageFlagBits::eUniformBuffer,
                VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE,
                vk::MemoryPropertyFlagBits::eHostVisible,
                VMA_ALLOCATION_CREATE_MAPPED_BIT |
                (randomAccess
                 ? VMA_ALLOCATION_CREATE_HOST_ACCESS_RANDOM_BIT
                 : VMA_ALLOCATION_CREATE_HOST_ACCESS_SEQUENTIAL_WRITE_BIT),name);
    }

    void Allocator::DestroyBuffer(const AllocatedBuffer &buffer) const {
        vmaDestroyBuffer(_allocator, buffer.buffer, buffer.alloc);
    }


    std::shared_ptr<AllocatedImage> Allocator::AllocateImage(
            vk::ImageCreateInfo &createInfo, const VmaMemoryUsage memoryUsage,
            const vk::MemoryPropertyFlags requiredFlags,const std::string& name) const {
      
        std::shared_ptr<AllocatedImage> result = std::shared_ptr<AllocatedImage>(new AllocatedImage(_allocator), [this](const AllocatedImage *ptr) {
            DestroyImage(*ptr);
            delete ptr;
        });

        VmaAllocationCreateInfo imageAllocInfo = {};

        imageAllocInfo.usage = memoryUsage;
        imageAllocInfo.requiredFlags = static_cast<VkMemoryPropertyFlags>(requiredFlags);

        const VkImageCreateInfo vmaImageCreateInfo = createInfo;
        VkImage vmaImage = result->image;

        vmaCreateImage(_allocator, &vmaImageCreateInfo, &imageAllocInfo, &vmaImage, &result->alloc,
                       nullptr);

        result->image = vmaImage;
        result->format = createInfo.format;
        result->extent = createInfo.extent;

      vmaSetAllocationName(_allocator,result->alloc,name.c_str());

        return result;
    }

    void Allocator::DestroyImage(const AllocatedImage &image) const {
        GetOwner()->GetVirtualDevice().destroyImageView(image.view);

        vmaDestroyImage(_allocator, image.image, image.alloc);
    }




}
