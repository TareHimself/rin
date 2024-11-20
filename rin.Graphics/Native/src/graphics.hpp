#pragma once
#define VULKAN_HPP_DISPATCH_LOADER_DYNAMIC 1
#include "macro.hpp"
#include <vulkan/vulkan.hpp>
#include <vk_mem_alloc.h>
#include <GLFW/glfw3.h>
//using json = nlohmann::json;

struct WindowCreateOptions
{
    bool resizable = true;
    
    bool visible = true;
    
    bool decorated = true;
    
    bool focused = true;
    
    bool floating = false;
    
    bool maximized = false;
    
    bool cursorCentered = false;
};

EXPORT_DECL int initWindows();

EXPORT_DECL void stopWindows();

EXPORT_DECL GLFWwindow* createWindow(int width, int height, const char* name,const WindowCreateOptions * options);

EXPORT_DECL void destroyWindow(GLFWwindow* window);

EXPORT_DECL void pollWindows();

using GlfwKeyCallback = void(__stdcall *)(GLFWwindow* window, int key, int scancode, int action, int mods);
using GlfwCursorPosCallback = void(__stdcall *)(GLFWwindow* window, double x, double y);
using GlfwMouseButtonCallback = void(__stdcall *)(GLFWwindow* window, int button, int action, int mods);
using GlfwWindowFocusCallback = void(__stdcall *)(GLFWwindow* window, int focused);
using GlfwScrollCallback = void(__stdcall *)(GLFWwindow* window, const double dx, const double dy);
using GlfwWindowSizeCallback = void(__stdcall *)(GLFWwindow* window, const int width, const int height);
using GlfwWindowCloseCallback = void(__stdcall *)(GLFWwindow* window);
using GlfwCharCallback =  void(__stdcall *)(GLFWwindow* window, unsigned int codepoint, int mods);
using GlfwMaximizedCallback =  void(__stdcall *)(GLFWwindow* window, int maximized);
using GlfwRefreshCallback =  void(__stdcall *)(GLFWwindow* window);
using GlfwMinimizeCallback = void(__stdcall *)(GLFWwindow* window, int iconified);
using GlfwDropCallback = void(__stdcall *)(GLFWwindow* window, int path_count, const char* paths[]);



EXPORT_DECL void setWindowCallbacks(GLFWwindow* window, const GlfwKeyCallback keyCallback,
                                 const GlfwCursorPosCallback cursorPosCallback,
                                 const GlfwMouseButtonCallback mouseButtonCallback,
                                 const GlfwWindowFocusCallback windowFocusCallback,
                                 const GlfwScrollCallback scrollCallback,
                                 const GlfwWindowSizeCallback windowSizeCallback,
                                 const GlfwWindowCloseCallback windowCloseCallback,
                                 const GlfwCharCallback charCallback,
                                 const GlfwMaximizedCallback maximizedCallback,
                                 const GlfwRefreshCallback refreshCallback,
                                 const GlfwMinimizeCallback minimizeCallback,
                                 const GlfwDropCallback dropCallback
                                 //,const GlfwDropCallback dropCallback
                                 );

EXPORT_DECL void getWindowMousePosition(GLFWwindow* window, double * x,double * y);

EXPORT_DECL void setWindowMousePosition(GLFWwindow* window, double x,double y);

EXPORT_DECL void getWindowPixelSize(GLFWwindow* window, int * x,int * y);

EXPORT_DECL void setWindowSize(GLFWwindow* window, int x,int y);

EXPORT_DECL void getWindowPosition(GLFWwindow* window, int * x,int * y);

EXPORT_DECL void setWindowPosition(GLFWwindow* window, int x,int y);

EXPORT_DECL void setWindowTitle(GLFWwindow* window,const char * title);

EXPORT_DECL void setWindowFullScreen(GLFWwindow* window,int fullscreen);

EXPORT_DECL int getWindowFullScreen(GLFWwindow* window);

EXPORT_DECL void createVulkanInstance(GLFWwindow * inWindow, VkInstance* outInstance,VkDevice* outDevice,VkPhysicalDevice* outPhysicalDevice,VkQueue* outGraphicsQueue, uint32_t* outGraphicsQueueFamily,VkQueue* outTransferQueue, uint32_t* outTransferQueueFamily,VkSurfaceKHR * outSurface,VkDebugUtilsMessengerEXT * outMessenger);

EXPORT_DECL void destroyVulkanMessenger(VkInstance instance,VkDebugUtilsMessengerEXT messenger);

using CreateSwapchainCallback = void(__stdcall *)(uintptr_t swapchain,void * swapchainImages,uint32_t numSwapchainImages,void * swapchainImageViews,uint32_t numSwapchainImageViews);
EXPORT_DECL void createSwapchain(VkDevice device,VkPhysicalDevice physicalDevice,VkSurfaceKHR surface,int swapchainFormat,int colorSpace,int presentMode,uint32_t width,uint32_t height,CreateSwapchainCallback callback);


void createBuffer(VmaAllocator allocator,VkBuffer * buffer,VmaAllocation * allocation,const size_t allocSize, const vk::BufferUsageFlags usage, const VmaMemoryUsage memoryUsage,
                  const vk::MemoryPropertyFlags requiredFlags, const VmaAllocationCreateFlags flags, const char * name);

EXPORT_DECL void * allocatorCreate(VkInstance instance,VkDevice device,VkPhysicalDevice physicalDevice);

EXPORT_DECL void allocatorDestroy(void * allocator);

EXPORT_DECL void allocatorNewBuffer(VkBuffer * buffer, void** allocation, unsigned long size, void* allocator,
                                int sequentialWrite, int preferHost, int usageFlags, int memoryPropertyFlags,
                                int mapped, const char* debugName);

EXPORT_DECL void allocatorNewImage(VkImage* image,void ** allocation,VkImageCreateInfo * createInfo,void * allocator, const char * debugName);

EXPORT_DECL void allocatorFreeBuffer(VkBuffer buffer,void * allocation,void * allocator);

EXPORT_DECL void allocatorFreeImage(VkImage image,void * allocation,void * allocator);

EXPORT_DECL void allocatorCopyToBuffer(void * allocator,void * allocation,void * data,unsigned long size,unsigned long offset);

EXPORT_DECL void createSurface(VkInstance instance,GLFWwindow* window,VkSurfaceKHR * outSurface);

EXPORT_DECL void dVkCmdBindShadersEXT(VkCommandBuffer commandBuffer, 
   uint32_t stageCount, 
   VkShaderStageFlagBits* pStages, 
   VkShaderEXT* pShaders);

EXPORT_DECL void dVkCmdBeginRenderingKHR(VkCommandBuffer commandBuffer, 
    VkRenderingInfo* pRenderingInfo);

EXPORT_DECL void dVkCmdSetPolygonModeEXT(VkCommandBuffer commandBuffer, VkPolygonMode polygonMode);

EXPORT_DECL void dVkCmdSetLogicOpEXT(VkCommandBuffer commandBuffer, VkLogicOp logicOp);

EXPORT_DECL void dVkCmdSetVertexInputEXT( VkCommandBuffer commandBuffer, 
        uint32_t vertexBindingDescriptionCount, 
    VkVertexInputBindingDescription2EXT* pVertexBindingDescriptions, 
        uint32_t vertexAttributeDescriptionCount, 
    VkVertexInputAttributeDescription2EXT* pVertexAttributeDescriptions);

EXPORT_DECL void dVkCmdSetLogicOpEnableEXT(VkCommandBuffer commandBuffer, uint32_t logicOpEnable);

EXPORT_DECL void dVkCmdSetColorBlendEnableEXT(
    VkCommandBuffer commandBuffer, 
    uint32_t firstAttachment, 
    uint32_t attachmentCount, 
    uint32_t* pColorBlendEnables);

EXPORT_DECL void dVkCmdSetColorBlendEquationEXT(
    VkCommandBuffer commandBuffer, 
    uint32_t firstAttachment, 
    uint32_t attachmentCount, 
    VkColorBlendEquationEXT* pColorBlendEquations);

EXPORT_DECL void dVkCmdSetColorWriteMaskEXT(
    VkCommandBuffer commandBuffer, 
    uint32_t firstAttachment, 
    uint32_t attachmentCount, 
    VkColorComponentFlags* pColorWriteMasks);

EXPORT_DECL VkResult dVkCreateShadersEXT(
        VkDevice device,
        uint32_t createInfoCount,
        VkShaderCreateInfoEXT* pCreateInfos,
        VkAllocationCallbacks* pAllocator,
        VkShaderEXT* pShaders);


EXPORT_DECL void dVkDestroyShaderEXT(
        VkDevice device,
        VkShaderEXT shader,
        VkAllocationCallbacks* pAllocator);

EXPORT_DECL void dVkCmdSetRasterizationSamplesEXT( VkCommandBuffer commandBuffer, 
    VkSampleCountFlagBits rasterizationSamples);

EXPORT_DECL void dVkCmdSetAlphaToCoverageEnableEXT(VkCommandBuffer commandBuffer, uint32_t alphaToCoverageEnable);

EXPORT_DECL void dVkCmdSetAlphaToOneEnableEXT(VkCommandBuffer commandBuffer, uint32_t alphaToOneEnable);

EXPORT_DECL void dVkCmdSetSampleMaskEXT(
    VkCommandBuffer commandBuffer, 
    VkSampleCountFlagBits samples, 
    uint32_t* pSampleMask);
