#pragma once
#define VULKAN_HPP_DISPATCH_LOADER_DYNAMIC 1
#include "macro.hpp"
#include <vulkan/vulkan.hpp>
#include <vk_mem_alloc.h>

RIN_NATIVE_API void createVulkanInstance(std::uint64_t windowHandle, VkInstance* outInstance, VkDevice* outDevice, VkPhysicalDevice* outPhysicalDevice, VkQueue* outGraphicsQueue, uint32_t* outGraphicsQueueFamily, VkQueue* outTransferQueue, uint32_t* outTransferQueueFamily, VkSurfaceKHR * outSurface, VkDebugUtilsMessengerEXT * outMessenger);

RIN_NATIVE_API void destroyVulkanMessenger(VkInstance instance,VkDebugUtilsMessengerEXT messenger);

void createBuffer(VmaAllocator allocator,VkBuffer * buffer,VmaAllocation * allocation,const size_t allocSize, const vk::BufferUsageFlags usage, const VmaMemoryUsage memoryUsage,
                  const vk::MemoryPropertyFlags requiredFlags, const VmaAllocationCreateFlags flags, const char * name);

RIN_NATIVE_API void * allocatorCreate(VkInstance instance,VkDevice device,VkPhysicalDevice physicalDevice);

RIN_NATIVE_API void allocatorDestroy(void * allocator);

RIN_NATIVE_API void allocatorNewBuffer(VkBuffer * buffer, void** allocation, size_t size, void* allocator,
                                int sequentialWrite, int preferHost, int usageFlags, int memoryPropertyFlags,
                                int mapped, const char* debugName);

RIN_NATIVE_API void allocatorNewImage(VkImage* image,void ** allocation,VkImageCreateInfo * createInfo,void * allocator, const char * debugName);

RIN_NATIVE_API void allocatorFreeBuffer(VkBuffer buffer,void * allocation,void * allocator);

RIN_NATIVE_API void allocatorFreeImage(VkImage image,void * allocation,void * allocator);

RIN_NATIVE_API void allocatorCopyToBuffer(void * allocator,void * allocation,void * data,size_t size,size_t offset);

RIN_NATIVE_API void dVkCmdBindShadersEXT(VkCommandBuffer commandBuffer,
   uint32_t stageCount,
   VkShaderStageFlagBits* pStages,
   VkShaderEXT* pShaders);
