#define VMA_IMPLEMENTATION
#include <vengine/drawing/DrawingSubsystem.hpp>
#include "VkBootstrap.h"
#include <vengine/drawing/Allocator.hpp>

namespace vengine::drawing {

void * Allocation::GetMappedData() const {
  return _allocation->GetMappedData();
}

Allocation::operator VmaAllocation_T*() const {
  return _allocation;
}


void Allocator::Init(DrawingSubsystem * outer) {
  Object<DrawingSubsystem>::Init(outer);
  auto allocatorCreateInfo = VmaAllocatorCreateInfo{};
  allocatorCreateInfo.flags = VMA_ALLOCATOR_CREATE_BUFFER_DEVICE_ADDRESS_BIT;
  allocatorCreateInfo.device = outer->GetVirtualDevice();
  allocatorCreateInfo.physicalDevice = outer->GetPhysicalDevice();
  allocatorCreateInfo.instance = outer->GetVulkanInstance();
  allocatorCreateInfo.vulkanApiVersion = VKB_MAKE_VK_VERSION(0, 1, 3, 0);
  
  vmaCreateAllocator(&allocatorCreateInfo,&_allocator);
}

void Allocator::BeforeDestroy() {
  Object<DrawingSubsystem>::BeforeDestroy();
  vmaDestroyAllocator(_allocator);
}

Managed<AllocatedBuffer> Allocator::CreateBuffer(const size_t allocSize,
                                                         const vk::BufferUsageFlags usage, const VmaMemoryUsage memoryUsage,
                                                         const vk::MemoryPropertyFlags requiredFlags, const VmaAllocationCreateFlags flags) const {
  const auto bufferInfo = vk::BufferCreateInfo({}, allocSize,
                                               usage);
  //vma::AllocationCreateFlagBits::eMapped
  VmaAllocationCreateInfo vmaAllocInfo = {};
  vmaAllocInfo.flags = flags;
  vmaAllocInfo.usage = memoryUsage;
  vmaAllocInfo.requiredFlags = static_cast<VkMemoryPropertyFlags>(requiredFlags);

  
  Managed<AllocatedBuffer> result = Managed<AllocatedBuffer>(new AllocatedBuffer,[this](const AllocatedBuffer * ptr) {
    DestroyBuffer(*ptr);
    delete ptr;
  });
  
  const VkBufferCreateInfo vmaBufferCreateInfo = bufferInfo;
  VkBuffer rawBuffer = result->buffer;

  vmaCreateBuffer(_allocator,&vmaBufferCreateInfo,&vmaAllocInfo,&rawBuffer,&result->alloc._allocation,nullptr);
  result->buffer = rawBuffer;
  result->size = allocSize;
  
  return result;
}

Managed<AllocatedBuffer> Allocator::CreateTransferCpuGpuBuffer(
    const size_t size,
    bool randomAccess) const {
  
  return CreateBuffer(size,
                      vk::BufferUsageFlagBits::eTransferSrc,
                      VMA_MEMORY_USAGE_AUTO_PREFER_HOST,
                      vk::MemoryPropertyFlagBits::eHostVisible |
                      vk::MemoryPropertyFlagBits::eHostCoherent,
                      VMA_ALLOCATION_CREATE_MAPPED_BIT | (randomAccess
                        ? VMA_ALLOCATION_CREATE_HOST_ACCESS_RANDOM_BIT
                        : VMA_ALLOCATION_CREATE_HOST_ACCESS_SEQUENTIAL_WRITE_BIT));
}

Managed<AllocatedBuffer> Allocator::CreateUniformCpuGpuBuffer(
    const size_t size,
    bool randomAccess) const {
  return CreateBuffer(
      size, vk::BufferUsageFlagBits::eUniformBuffer,
      VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE,
      vk::MemoryPropertyFlagBits::eHostVisible,
      VMA_ALLOCATION_CREATE_MAPPED_BIT |
      (randomAccess
         ? VMA_ALLOCATION_CREATE_HOST_ACCESS_RANDOM_BIT
         : VMA_ALLOCATION_CREATE_HOST_ACCESS_SEQUENTIAL_WRITE_BIT));
}

void Allocator::DestroyBuffer(const AllocatedBuffer &buffer) const {
  vmaDestroyBuffer(_allocator,buffer.buffer,buffer.alloc);
}


Managed<AllocatedImage> Allocator::AllocateImage(
    vk::ImageCreateInfo &createInfo, const VmaMemoryUsage memoryUsage,
    const vk::MemoryPropertyFlags requiredFlags) const {

  Managed<AllocatedImage> result = Managed<AllocatedImage>(new AllocatedImage,[this](const AllocatedImage * ptr) {
    DestroyImage(*ptr);
    delete ptr;
  });
  
  VmaAllocationCreateInfo imageAllocInfo = {};
  
  imageAllocInfo.usage = memoryUsage;
  imageAllocInfo.requiredFlags = static_cast<VkMemoryPropertyFlags>(requiredFlags);

  const VkImageCreateInfo vmaImageCreateInfo = createInfo;
  VkImage vmaImage = result->image;
  
  vmaCreateImage(_allocator,&vmaImageCreateInfo,&imageAllocInfo,&vmaImage,&result->alloc._allocation,nullptr);
  
  result->image = vmaImage;
  result->format = createInfo.format;
  result->extent = createInfo.extent;

  return result;
}

void Allocator::DestroyImage(const AllocatedImage &image) const {
  GetOuter()->GetVirtualDevice().destroyImageView(image.view);
  
  vmaDestroyImage(_allocator,image.image,image.alloc);
}

}
