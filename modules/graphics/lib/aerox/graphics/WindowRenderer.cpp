#include "aerox/graphics/WindowRenderer.hpp"

#include <VkBootstrap.h>
#include "aerox/graphics/GraphicsModule.hpp"
#include "aerox/graphics/Frame.hpp"
#include <iostream>
#include <SDL3/SDL_vulkan.h>

namespace aerox::graphics
{
    void WindowRenderer::Init()
    {
        _swapchainSize = _window->GetSize().Cast<uint32_t>();
        CreateSwapchain();
        CreateFrames();
        _resizeHandle = _window->onResize->Add(this, &WindowRenderer::OnResize);
    }

    void WindowRenderer::CreateSwapchain()
    {
        constexpr auto sFormat = vk::Format::eR32G32B32A32Sfloat;
        constexpr auto sColorSpace = vk::ColorSpaceKHR::eSrgbNonlinear;
        constexpr auto sPresentMode = vk::PresentModeKHR::eFifo;

        vkb::SwapchainBuilder swapchainBuilder{
            _graphicsModule->GetPhysicalDevice(), _graphicsModule->GetDevice(), _surface
        };
        vkb::Swapchain vkbSwapchain = swapchainBuilder
                                      .set_desired_format(
                                          vk::SurfaceFormatKHR(
                                              sFormat,
                                              sColorSpace))
                                      // .set_desired_present_mode(
                                      //     VK_PRESENT_MODE_FIFO_KHR)
                                      .set_desired_present_mode(static_cast<VkPresentModeKHR>(sPresentMode))
                                      .set_desired_extent(_swapchainSize.x, _swapchainSize.y)
                                      .add_image_usage_flags(
                                          VK_IMAGE_USAGE_TRANSFER_DST_BIT)
                                      .build()
                                      .value();

        {
            const auto items = vkbSwapchain.get_images().value();
            _swapchainImages.reserve(items.size());
            for (auto item : items)
                _swapchainImages.emplace_back(item);
        }
        {
            const auto items = vkbSwapchain.get_image_views().value();
            _swapchainViews.reserve(items.size());
            for (auto item : items)
                _swapchainViews.emplace_back(item);
        }
        _swapchainReady = true;
        _swapchain = vkbSwapchain.swapchain;
    }

    void WindowRenderer::DestroySwapchain()
    {
        _swapchainReady = false;
        if (_swapchain)
        {
            const auto device = _graphicsModule->GetDevice();

            for (const auto view : _swapchainViews)
            {
                device.destroyImageView(view);
            }

            _swapchainViews.clear();
            _swapchainImages.clear();

            device.destroySwapchainKHR(_swapchain);
            _swapchain = vk::SwapchainKHR();
        }
    }

    void WindowRenderer::CreateFrames()
    {
        _frames.reserve(NUM_FRAMES);
        for (auto i = 0; i < NUM_FRAMES; i++)
        {
            _frames.push_back( new Frame(this));
        }
    }

    void WindowRenderer::DestroyFrames()
    {
        for (const auto frame : _frames)
        {
            delete frame;
        }

        _frames.clear();
    }

    void WindowRenderer::OnResize(window::Window* window, const Vec2<int>& newSize)
    {
        _graphicsModule->WaitForDeviceIdle();
        DestroySwapchain();
        _swapchainSize = newSize.Cast<uint32_t>();
        if (_swapchainSize.x > 0 && _swapchainSize.y > 0)
        {
            CreateSwapchain();
        }
    }

    WindowRenderer::WindowRenderer(const vk::Instance& instance, window::Window* window, GraphicsModule* graphicsModule)
    {
        VkSurfaceKHR surf;
        SDL_Vulkan_CreateSurface(window->GetSDLWindow(),instance,nullptr,&surf);
        _surface = surf;
        _graphicsModule = graphicsModule;
        _window = window;
    }


    vk::SurfaceKHR WindowRenderer::GetSurface() const
    {
        return _surface;
    }

    window::Window* WindowRenderer::GetWindow() const
    {
        return _window;
    }

    GraphicsModule* WindowRenderer::GetModule() const
    {
        return _graphicsModule;
    }

    WindowRenderer::~WindowRenderer()
    {
        _resizeHandle.UnBind();
        DestroyFrames();
        DestroySwapchain();
        _graphicsModule->GetInstance().destroySurfaceKHR(_surface);
    }

    bool WindowRenderer::CanDraw() const
    {
        return _swapchainReady && _frames.size() == NUM_FRAMES;
    }

    void WindowRenderer::Draw()
    {
        if (!CanDraw()) return;

        auto frame = _frames[_framesDrawn % NUM_FRAMES];
        auto device = _graphicsModule->GetDevice();

        frame->WaitForLastDraw();

        frame->Reset();

        uint32_t swapchainIndex{0};

        if (const auto r = device.acquireNextImageKHR(_swapchain, std::numeric_limits<uint64_t>::max(),
                                                      frame->GetSwapchainSemaphore(), {},
                                                      &swapchainIndex); r != vk::Result::eSuccess)
        {
            return;
        }

        const auto cmd = frame->GetCommandBuffer();

        cmd.reset();

        if(onDraw->HasBindings())
        {
            cmd.begin({vk::CommandBufferUsageFlagBits::eOneTimeSubmit});

            onDraw->Invoke(frame);

            if(onCopy->HasBindings())
            {
                GraphicsModule::ImageBarrier(cmd, _swapchainImages[swapchainIndex], vk::ImageLayout::eUndefined,
                                         vk::ImageLayout::eTransferDstOptimal,
                                         ImageBarrierOptions());

                onCopy->Invoke(frame,_swapchainImages[swapchainIndex],vk::Extent2D{_swapchainSize.x,_swapchainSize.y});

                GraphicsModule::ImageBarrier(cmd, _swapchainImages[swapchainIndex], vk::ImageLayout::eTransferDstOptimal,
                                             vk::ImageLayout::ePresentSrcKHR,
                                             ImageBarrierOptions());
            }
            else
            {
                GraphicsModule::ImageBarrier(cmd, _swapchainImages[swapchainIndex], vk::ImageLayout::eUndefined,
                                             vk::ImageLayout::ePresentSrcKHR,
                                             ImageBarrierOptions());
            }

            cmd.end();
        }
        else
        {
            cmd.begin({vk::CommandBufferUsageFlagBits::eOneTimeSubmit});
            GraphicsModule::ImageBarrier(cmd, _swapchainImages[swapchainIndex], vk::ImageLayout::eUndefined,
                                             vk::ImageLayout::ePresentSrcKHR,
                                             ImageBarrierOptions());
            cmd.end();
        }

        vk::CommandBufferSubmitInfo cmdSubmits[] = {vk::CommandBufferSubmitInfo{cmd}};
        vk::SemaphoreSubmitInfo signalSemaphoreSubmits[] = {vk::SemaphoreSubmitInfo{frame->GetRenderSemaphore(),1,vk::PipelineStageFlagBits2::eAllGraphics}};
        vk::SemaphoreSubmitInfo waitSemaphoreSubmits[] = {vk::SemaphoreSubmitInfo{frame->GetSwapchainSemaphore(),1,vk::PipelineStageFlagBits2::eColorAttachmentOutput}};

        try
        {
            _graphicsModule->GetQueue().submit2(vk::SubmitInfo2({}, waitSemaphoreSubmits,  cmdSubmits,signalSemaphoreSubmits), frame->GetRenderFence());
            vk::Semaphore waitSem[] = {frame->GetRenderSemaphore()};
            vk::SwapchainKHR swapchains[] = {_swapchain};
            auto r = _graphicsModule->GetQueue().presentKHR(vk::PresentInfoKHR{waitSem,swapchains,swapchainIndex});
        }
        catch (...)
        {
            std::cerr << "Present Failed" << std::endl;
        }
    
        _framesDrawn++;
    }
}
