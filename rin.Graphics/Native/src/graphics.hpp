#pragma once
#define VULKAN_HPP_DISPATCH_LOADER_DYNAMIC 1
#include "macro.hpp"
#include <vulkan/vulkan.hpp>
#include <vk_mem_alloc.h>
//using json = nlohmann::json;

EXPORT void graphicsCreateVulkanInstance(void * inWindow, void ** outInstance,void ** outDevice,void ** outPhysicalDevice,void ** outQueue,uint32_t * outQueueFamily,uintptr_t * outSurface,uintptr_t * outMessenger);

EXPORT void graphicsDestroyVulkanMessenger(void * instance,uintptr_t messenger);

using CreateSwapchainCallback = void(__stdcall *)(uintptr_t swapchain,void * swapchainImages,uint32_t numSwapchainImages,void * swapchainImageViews,uint32_t numSwapchainImageViews);
EXPORT void graphicsCreateSwapchain(void * device,void * physicalDevice,uintptr_t surface,int swapchainFormat,int colorSpace,int presentMode,uint32_t width,uint32_t height,CreateSwapchainCallback callback);


void createBuffer(VmaAllocator allocator,VkBuffer * buffer,VmaAllocation * allocation,const size_t allocSize, const vk::BufferUsageFlags usage, const VmaMemoryUsage memoryUsage,
                  const vk::MemoryPropertyFlags requiredFlags, const VmaAllocationCreateFlags flags, const char * name);

EXPORT void * graphicsAllocatorCreate(void * instance,void * device,void * physicalDevice);

EXPORT void graphicsAllocatorDestroy(void * allocator);

EXPORT void graphicsAllocatorNewBuffer(uintptr_t* buffer, void** allocation, unsigned long size, void* allocator,
                                int sequentialWrite, int preferHost, int usageFlags, int memoryPropertyFlags,
                                int mapped, const char* debugName);

EXPORT void graphicsAllocatorNewImage(uintptr_t * image,void ** allocation,void * createInfo,void * allocator, const char * debugName);

EXPORT void graphicsAllocatorFreeBuffer(uintptr_t buffer,void * allocation,void * allocator);

EXPORT void graphicsAllocatorFreeImage(uintptr_t image,void * allocation,void * allocator);

EXPORT void graphicsAllocatorCopyToBuffer(void * allocator,void * allocation,void * data,unsigned long size,unsigned long offset);

EXPORT uintptr_t graphicsCreateSurface(void * instance,void * window);

EXPORT void graphicsVkCmdBindShadersEXT(VkCommandBuffer commandBuffer, 
   uint32_t stageCount, 
   VkShaderStageFlagBits* pStages, 
   VkShaderEXT* pShaders);

EXPORT void graphicsVkCmdBeginRenderingKHR(VkCommandBuffer commandBuffer, 
    VkRenderingInfo* pRenderingInfo);

EXPORT void graphicsVkCmdSetPolygonModeEXT(VkCommandBuffer commandBuffer, VkPolygonMode polygonMode);

EXPORT void graphicsVkCmdSetLogicOpEXT(VkCommandBuffer commandBuffer, VkLogicOp logicOp);

EXPORT void graphicsVkCmdSetVertexInputEXT( VkCommandBuffer commandBuffer, 
        uint32_t vertexBindingDescriptionCount, 
    VkVertexInputBindingDescription2EXT* pVertexBindingDescriptions, 
        uint32_t vertexAttributeDescriptionCount, 
    VkVertexInputAttributeDescription2EXT* pVertexAttributeDescriptions);

EXPORT void graphicsVkCmdSetLogicOpEnableEXT(VkCommandBuffer commandBuffer, uint32_t logicOpEnable);

EXPORT void graphicsVkCmdSetColorBlendEnableEXT(
    VkCommandBuffer commandBuffer, 
    uint32_t firstAttachment, 
    uint32_t attachmentCount, 
    uint32_t* pColorBlendEnables);

EXPORT void graphicsVkCmdSetColorBlendEquationEXT(
    VkCommandBuffer commandBuffer, 
    uint32_t firstAttachment, 
    uint32_t attachmentCount, 
    VkColorBlendEquationEXT* pColorBlendEquations);

EXPORT void graphicsVkCmdSetColorWriteMaskEXT(
    VkCommandBuffer commandBuffer, 
    uint32_t firstAttachment, 
    uint32_t attachmentCount, 
    VkColorComponentFlags* pColorWriteMasks);

EXPORT VkResult graphicsVkCreateShadersEXT(
        VkDevice device,
        uint32_t createInfoCount,
        VkShaderCreateInfoEXT* pCreateInfos,
        VkAllocationCallbacks* pAllocator,
        VkShaderEXT* pShaders);


EXPORT void graphicsVkDestroyShaderEXT(
        VkDevice device,
        VkShaderEXT shader,
        VkAllocationCallbacks* pAllocator);

EXPORT void graphicsVkCmdSetRasterizationSamplesEXT( VkCommandBuffer commandBuffer, 
    VkSampleCountFlagBits rasterizationSamples);

EXPORT void graphicsVkCmdSetAlphaToCoverageEnableEXT(VkCommandBuffer commandBuffer, uint32_t alphaToCoverageEnable);

EXPORT void graphicsVkCmdSetAlphaToOneEnableEXT(VkCommandBuffer commandBuffer, uint32_t alphaToOneEnable);

EXPORT void graphicsVkCmdSetSampleMaskEXT(
    VkCommandBuffer commandBuffer, 
    VkSampleCountFlagBits samples, 
    uint32_t* pSampleMask);
