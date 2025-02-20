#pragma once
#include <vector>

#include "GraphicsModule.h"
#include "rin/core/memory.h"
#include "rin/io/Window.h"
#define FRAMES_IN_FLIGHT 1
namespace rin::rhi
{
    class GraphImagePool;
    class Frame;
    class GraphBuilder;
    class WindowRenderer : public Disposable
    {
        io::Window * _window{};
        std::vector<Shared<Frame>> _frames{};
        uint64_t _rendered{0};
        vk::SurfaceKHR _surface{};
        vk::SwapchainKHR _swapchain{};
        vk::Extent3D _swapchainExtent{};
        std::vector<vk::Image> _images{};
        std::vector<vk::ImageView> _views{};
        GraphicsModule * _module{};
        Shared<GraphImagePool> _graphImagePool{};
        vk::Viewport _viewport{};
        bool _resizePending{false};
        bool _resizing{false};
        std::mutex _resizeMutex{};
        friend GraphicsModule;
    protected:
        void Init();


        void TryResize();
        void CreateSwapchain();
        void DestroySwapchain();
        void CreateFrames();
        void DestroyFrames();
        void DrawFrame();
        void Draw();
        void RequestResize();
    public:

        WindowRenderer(GraphicsModule * module,io::Window * window);
        
        bool ShouldDraw() const;
        
        Frame * GetCurrentFrame() const;

        vk::Extent3D GetSwapchainExtent() const;

        GraphicsModule * GetGraphicsModule() const;
    protected:
        void OnDispose() override;
    public:
        DEFINE_DELEGATE_LIST(onBuildGraph,GraphBuilder&)

        DEFINE_DELEGATE_LIST(onResize,const Vec2<uint32_t>&)
    };
}
