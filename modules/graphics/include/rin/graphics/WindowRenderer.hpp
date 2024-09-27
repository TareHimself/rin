#pragma once
#include <vulkan/vulkan.hpp>

#include "rin/core/RinBase.hpp"
#include "rin/window/Window.hpp"
class Frame;
class GraphicsModule;
#define NUM_FRAMES 2
class WindowRenderer : RinBase {
    Window * _window{};
    vk::SurfaceKHR _surface{};
    GraphicsModule * _graphicsModule{};
    std::vector<Frame*> _frames{};
    size_t _framesDrawn{0};
    std::vector<vk::ImageView> _swapchainViews{};
    std::vector<vk::Image> _swapchainImages{};
    vk::SwapchainKHR _swapchain{};
    Vec2<uint32_t> _swapchainSize{0};

    DelegateListHandle _resizeHandle{};

    bool _swapchainReady = false;
protected:
    void Init();
    void CreateSwapchain();
    void DestroySwapchain();
    void CreateFrames();
    void DestroyFrames();
    void OnResize(Window * window,const Vec2<int>& newSize);
public:
    friend GraphicsModule;
    explicit WindowRenderer(const vk::Instance& instance,Window * window,GraphicsModule * graphicsModule);

    vk::SurfaceKHR GetSurface() const;
    Window* GetWindow() const;

    GraphicsModule * GetModule() const;

    ~WindowRenderer() override;

    bool CanDraw() const;

    void Draw();

    DEFINE_DELEGATE_LIST(onDraw,Frame*)
    DEFINE_DELEGATE_LIST(onCopy,Frame*,const vk::Image&,const vk::Extent2D&)
};

