#pragma once
#define VULKAN_HPP_DISPATCH_LOADER_DYNAMIC 1
#include "macro.hpp"
#include <vulkan/vulkan.hpp>
#include <vk_mem_alloc.h>
#include <GLFW/glfw3.h>
//using json = nlohmann::json;

EXPORT_DECL void graphicsCreateVulkanInstance(GLFWwindow * inWindow, VkInstance* outInstance,VkDevice* outDevice,VkPhysicalDevice* outPhysicalDevice,VkQueue* outGraphicsQueue, uint32_t* outGraphicsQueueFamily,VkQueue* outTransferQueue, uint32_t* outTransferQueueFamily,VkSurfaceKHR * outSurface,VkDebugUtilsMessengerEXT * outMessenger);

EXPORT_DECL void graphicsDestroyVulkanMessenger(VkInstance instance,VkDebugUtilsMessengerEXT messenger);

using CreateSwapchainCallback = void(__stdcall *)(uintptr_t swapchain,void * swapchainImages,uint32_t numSwapchainImages,void * swapchainImageViews,uint32_t numSwapchainImageViews);
EXPORT_DECL void graphicsCreateSwapchain(VkDevice device,VkPhysicalDevice physicalDevice,VkSurfaceKHR surface,int swapchainFormat,int colorSpace,int presentMode,uint32_t width,uint32_t height,CreateSwapchainCallback callback);


void createBuffer(VmaAllocator allocator,VkBuffer * buffer,VmaAllocation * allocation,const size_t allocSize, const vk::BufferUsageFlags usage, const VmaMemoryUsage memoryUsage,
                  const vk::MemoryPropertyFlags requiredFlags, const VmaAllocationCreateFlags flags, const char * name);

EXPORT_DECL void * graphicsAllocatorCreate(VkInstance instance,VkDevice device,VkPhysicalDevice physicalDevice);

EXPORT_DECL void graphicsAllocatorDestroy(void * allocator);

EXPORT_DECL void graphicsAllocatorNewBuffer(VkBuffer * buffer, void** allocation, unsigned long size, void* allocator,
                                int sequentialWrite, int preferHost, int usageFlags, int memoryPropertyFlags,
                                int mapped, const char* debugName);

EXPORT_DECL void graphicsAllocatorNewImage(VkImage* image,void ** allocation,VkImageCreateInfo * createInfo,void * allocator, const char * debugName);

EXPORT_DECL void graphicsAllocatorFreeBuffer(VkBuffer buffer,void * allocation,void * allocator);

EXPORT_DECL void graphicsAllocatorFreeImage(VkImage image,void * allocation,void * allocator);

EXPORT_DECL void graphicsAllocatorCopyToBuffer(void * allocator,void * allocation,void * data,unsigned long size,unsigned long offset);

EXPORT_DECL void graphicsCreateSurface(VkInstance instance,GLFWwindow* window,VkSurfaceKHR * outSurface);

EXPORT_DECL void graphicsVkCmdBindShadersEXT(VkCommandBuffer commandBuffer, 
   uint32_t stageCount, 
   VkShaderStageFlagBits* pStages, 
   VkShaderEXT* pShaders);

EXPORT_DECL void graphicsVkCmdBeginRenderingKHR(VkCommandBuffer commandBuffer, 
    VkRenderingInfo* pRenderingInfo);

EXPORT_DECL void graphicsVkCmdSetPolygonModeEXT(VkCommandBuffer commandBuffer, VkPolygonMode polygonMode);

EXPORT_DECL void graphicsVkCmdSetLogicOpEXT(VkCommandBuffer commandBuffer, VkLogicOp logicOp);

EXPORT_DECL void graphicsVkCmdSetVertexInputEXT( VkCommandBuffer commandBuffer, 
        uint32_t vertexBindingDescriptionCount, 
    VkVertexInputBindingDescription2EXT* pVertexBindingDescriptions, 
        uint32_t vertexAttributeDescriptionCount, 
    VkVertexInputAttributeDescription2EXT* pVertexAttributeDescriptions);

EXPORT_DECL void graphicsVkCmdSetLogicOpEnableEXT(VkCommandBuffer commandBuffer, uint32_t logicOpEnable);

EXPORT_DECL void graphicsVkCmdSetColorBlendEnableEXT(
    VkCommandBuffer commandBuffer, 
    uint32_t firstAttachment, 
    uint32_t attachmentCount, 
    uint32_t* pColorBlendEnables);

EXPORT_DECL void graphicsVkCmdSetColorBlendEquationEXT(
    VkCommandBuffer commandBuffer, 
    uint32_t firstAttachment, 
    uint32_t attachmentCount, 
    VkColorBlendEquationEXT* pColorBlendEquations);

EXPORT_DECL void graphicsVkCmdSetColorWriteMaskEXT(
    VkCommandBuffer commandBuffer, 
    uint32_t firstAttachment, 
    uint32_t attachmentCount, 
    VkColorComponentFlags* pColorWriteMasks);

EXPORT_DECL VkResult graphicsVkCreateShadersEXT(
        VkDevice device,
        uint32_t createInfoCount,
        VkShaderCreateInfoEXT* pCreateInfos,
        VkAllocationCallbacks* pAllocator,
        VkShaderEXT* pShaders);


EXPORT_DECL void graphicsVkDestroyShaderEXT(
        VkDevice device,
        VkShaderEXT shader,
        VkAllocationCallbacks* pAllocator);

EXPORT_DECL void graphicsVkCmdSetRasterizationSamplesEXT( VkCommandBuffer commandBuffer, 
    VkSampleCountFlagBits rasterizationSamples);

EXPORT_DECL void graphicsVkCmdSetAlphaToCoverageEnableEXT(VkCommandBuffer commandBuffer, uint32_t alphaToCoverageEnable);

EXPORT_DECL void graphicsVkCmdSetAlphaToOneEnableEXT(VkCommandBuffer commandBuffer, uint32_t alphaToOneEnable);

EXPORT_DECL void graphicsVkCmdSetSampleMaskEXT(
    VkCommandBuffer commandBuffer, 
    VkSampleCountFlagBits samples, 
    uint32_t* pSampleMask);
