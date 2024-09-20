#pragma once
#include <vulkan/vulkan.hpp>
#include "ImageBarrierOptions.hpp"
#include "aerox/core/Module.hpp"
#include "aerox/window/WindowModule.hpp"
#include <future>
#include <functional>
#include <optional>
#include "Allocator.hpp"
#include "descriptors/DescriptorLayoutStore.hpp"

namespace aerox::graphics
{
    class ResourceManager;
}

namespace aerox::graphics
{
    class ShaderManager;
}

namespace aerox::graphics
{
    class DeviceImage;
    class WindowRenderer;


    class GraphicsModule : public Module
    {
        window::WindowModule * _windowModule = nullptr;
        std::unordered_map<window::Window*,WindowRenderer *> _renderers{};
#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
        VkDebugUtilsMessengerEXT _debugMessenger{};
#endif

        vk::Instance _instance{};
        vk::Device _device{};
        vk::PhysicalDevice _physicalDevice{};
        vk::Queue _queue{};
        uint32_t _queueFamily{0};

        vk::CommandBuffer _immediateCommandBuffer{};
        vk::CommandPool _immediateCommandPool{};
        vk::Fence _immediateFence{};

        DescriptorLayoutStore _descriptorLayoutStore{};
        
        DelegateListHandle _onWindowCreatedHandle{};
        DelegateListHandle _onWindowDestroyedHandle{};
        DelegateListHandle _onTickHandle{};
        void InitVulkan(window::Window * window);
        std::list<std::function<void()>> _pendingTasks;

        std::unique_ptr<Allocator> _allocator{};
        Shared<ShaderManager> _shaderManager{};
        Shared<ResourceManager> _resourceManager{};
        
    protected:
        void Startup(GRuntime* runtime) override;
        void Shutdown(GRuntime* runtime) override;
        void RegisterRequiredModules() override;

        void OnWindowCreated(window::Window * window);
        void OnWindowDestroyed(window::Window * window);

    public:
        GraphicsModule();
        static GraphicsModule * Get();
        std::string GetName() override;
    
        bool IsDependentOn(Module* module) override;
        
        static vk::DispatchLoaderDynamic dispatchLoader;
        
        vk::Instance GetInstance() const;

        vk::Device GetDevice() const;

        vk::PhysicalDevice GetPhysicalDevice() const;

        vk::Queue GetQueue() const;

        uint32_t GetQueueFamily() const;

        void WaitForDeviceIdle() const;

        ShaderManager * GetShaderManager() const;

        ResourceManager * GetResourceManager() const;

        Allocator * GetAllocator() const;

        DescriptorLayoutStore* GetDescriptorLayoutStore();

        static void ImageBarrier(vk::CommandBuffer cmd,vk::Image image,vk::ImageLayout from,vk::ImageLayout to,const ImageBarrierOptions& options = {});

        static void ImageBarrier(vk::CommandBuffer cmd,const Shared<DeviceImage>& image,vk::ImageLayout from,vk::ImageLayout to,const ImageBarrierOptions& options = {});

        void ImmediateSubmit(std::function<void(const vk::CommandBuffer&)>&& submit) const;

        static uint32_t DeriveMipLevels(const vk::Extent2D& extent);
        static uint32_t DeriveMipLevels(const vk::Extent3D& extent);

        static void CopyImageToImage(const vk::CommandBuffer& cmd,const vk::Image& src,const vk::Extent3D& srcExtent,const vk::Image& dst,const vk::Extent3D& dstExtent,const vk::Filter& filter = vk::Filter::eLinear);

        static vk::RenderingAttachmentInfo MakeRenderingAttachment(const Shared<DeviceImage>& image,const vk::ImageLayout& layout,const std::optional<vk::ClearValue>& clearValue = {});
        static vk::RenderingAttachmentInfo MakeRenderingAttachment(const vk::ImageView& view,const vk::ImageLayout& layout,const std::optional<vk::ClearValue>& clearValue = {});
        static vk::ImageCreateInfo MakeImageCreateInfo(const vk::Format& format,const vk::Extent3D& extent,vk::ImageUsageFlags usage);
        static vk::ImageViewCreateInfo MakeImageViewCreateInfo(const vk::Format& format,const vk::Image& image,const vk::ImageAspectFlags& aspect);
        static vk::ImageViewCreateInfo MakeImageViewCreateInfo(const Shared<DeviceImage>& image,const vk::ImageAspectFlags& aspect);

        static void GenerateMipMaps(const vk::CommandBuffer& cmd,const Shared<DeviceImage>& image,const vk::Extent3D& extent,const vk::Filter& filter);

        WindowRenderer * GetRenderer(window::Window * window) const;
        Shared<DeviceImage> CreateImage(const vk::Extent3D& extent,vk::Format format,vk::ImageUsageFlags usage, bool mipMap = false,
            const std::string& debugName = "Image") const;
        
        Shared<DeviceImage> CreateImage(const std::vector<std::byte>& data,const vk::Extent3D& extent,vk::Format format,vk::ImageUsageFlags usage, bool mipMap = false,const vk::Filter& mipMapFilter = vk::Filter::eLinear,
            const std::string& debugName = "Image") const;

        DEFINE_DELEGATE_LIST(onRendererCreated,WindowRenderer *)
        DEFINE_DELEGATE_LIST(onRendererDestroyed,WindowRenderer *)
    };
}
