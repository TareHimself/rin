#include "rin/rhi/WindowRenderer.h"
#include "rin/rhi/Frame.h"
#include "rin/rhi/graph/GraphBuilder.h"
#include "VkBootstrap.h"
#include "rin/rhi/CommandBuffer.h"
namespace rin::rhi
{
    void WindowRenderer::Init()
    {
        CreateSwapchain();
        CreateFrames();
    }
    void WindowRenderer::TryResize()
    {
        std::lock_guard g(_resizeMutex);
        if(_resizing) return;
        const auto size = _window->GetFrameBufferSize();
        if(_swapchainExtent.width == size.x && _swapchainExtent.height == size.y) return;
        _resizing = true;
        _module->WaitDeviceIdle();

        DestroySwapchain();
        CreateSwapchain();
        onResize->Invoke(size);
        _resizePending = false;
        _resizing = false;
    }
    void WindowRenderer::CreateSwapchain()
    {
        const auto size = _window->GetFrameBufferSize();
        _viewport.width = static_cast<float>(size.x);
        _viewport.height = static_cast<float>(size.y);
        _swapchainExtent = vk::Extent3D{size.x, size.y, 1};
        if(size.x == 0 || size.y == 0) return;

        vkb::SwapchainBuilder swapchainBuilder{_module->GetPhysicalDevice(), _module->GetDevice(), _surface};
        auto swapchain = swapchainBuilder
                         .set_desired_format(static_cast<VkSurfaceFormatKHR>(_module->GetSurfaceFormat()))
                         .set_desired_present_mode(static_cast<VkPresentModeKHR>(vk::PresentModeKHR::eFifo))
                         .set_desired_extent(size.x,size.y)
                         .add_image_usage_flags(VK_IMAGE_USAGE_TRANSFER_DST_BIT)
                         .build()
                         .value();

        for(auto item : swapchain.get_images().value())
        {
            _images.emplace_back(item);
        }

        for(auto item : swapchain.get_image_views().value())
        {
            _views.emplace_back(item);
        }

        _swapchain = swapchain;
    }
    void WindowRenderer::DestroySwapchain()
    {
        const auto device = _module->GetDevice();
        for(const auto& item : _views)
        {
            device.destroyImageView(item);
        }

        // for (const auto &item : _images)
        // {
        //     device.destroyImage(item);
        // }

        device.destroySwapchainKHR(_swapchain);
        _swapchainExtent = vk::Extent3D{};
        _images.clear();
        _views.clear();
    }
    void WindowRenderer::CreateFrames()
    {
        for(auto i = 0; i < FRAMES_IN_FLIGHT; i++)
        {
            _frames.emplace_back(shared<Frame>(this));
        }
    }
    void WindowRenderer::DestroyFrames()
    {
        for(const auto& frame : _frames)
        {
            frame->WaitForLastDraw();
        }
        _frames.clear();
    }
    void WindowRenderer::DrawFrame()
    {


        try
        {
            if(_resizePending && !_resizing)
            {
                TryResize();
            }

            {
                std::lock_guard g(_resizeMutex);
                if(!ShouldDraw()) return;

                const auto frame = GetCurrentFrame();

                const auto device = _module->GetDevice();

                frame->WaitForLastDraw();

                _graphImagePool->OnFrameStart();

                frame->Reset();

                uint32_t imageIndex{0};

                if(const auto result = device.acquireNextImageKHR(_swapchain,std::numeric_limits<uint64_t>::max(),frame->GetSwapchainSemaphore(),{},&imageIndex); result !=
                    vk::Result::eSuccess)
                {
                    RequestResize();
                    return;
                }
                auto cmd = frame->GetCommandBuffer();

                cmd.Reset();

                cmd.cmd.begin({vk::CommandBufferUsageFlagBits::eOneTimeSubmit});

                cmd
                    .RasterizerDiscard(false)
                    .DisableMultisampling();

                if(onBuildGraph->HasBindings())
                {
                    GraphBuilder builder{};
                    onBuildGraph->Invoke(builder);
                    if(const auto graph = builder.Compile(frame,_graphImagePool.get()))
                    {
                        cmd.ImageBarrier(_images[imageIndex],vk::ImageLayout::eUndefined,vk::ImageLayout::eTransferDstOptimal);
                        frame->onReset->Add([graph](Frame* _){
                            graph->Dispose();
                        });
                        graph->Execute();
                        cmd.ImageBarrier(_images[imageIndex],vk::ImageLayout::eTransferDstOptimal,vk::ImageLayout::ePresentSrcKHR);
                    }
                    else
                    {
                        cmd.ImageBarrier(_images[imageIndex],vk::ImageLayout::eUndefined,vk::ImageLayout::ePresentSrcKHR);
                    }
                }
                else
                {
                    cmd.ImageBarrier(_images[imageIndex],vk::ImageLayout::eUndefined,vk::ImageLayout::ePresentSrcKHR);
                }

                cmd.End();

                const auto queue = _module->GetGraphicsQueue();

                GraphicsModule::SubmitToQueue(
                    queue,
                    frame->GetRenderFence(),
                    std::vector{
                        vk::CommandBufferSubmitInfo{cmd, 0}
                    },
                    std::vector{
                        vk::SemaphoreSubmitInfo{
                            frame->GetRenderSemaphore(),
                            1,
                            vk::PipelineStageFlagBits2::eAllGraphics
                        }
                    },
                    std::vector{
                        vk::SemaphoreSubmitInfo{
                            frame->GetSwapchainSemaphore(),
                            1,
                            vk::PipelineStageFlagBits2::eAllGraphics
                        }
                    }
                );

                auto renderSemaphore = frame->GetRenderSemaphore();

                if(const auto result = queue.presentKHR(vk::PresentInfoKHR{{renderSemaphore}, {_swapchain}, {imageIndex}}); result != vk::Result::eSuccess)
                {
                    throw std::runtime_error("present failed");
                }
            }
            _rendered++;
        }
        catch(vk::OutOfDateKHRError&)
        {
            RequestResize();
        }catch(std::exception& e)
        {
            throw e;
        }
    }
    void WindowRenderer::Draw()
    {
        DrawFrame();
    }
    void WindowRenderer::RequestResize()
    {
        _resizePending = true;
    }
    bool WindowRenderer::ShouldDraw() const
    {
        return !_resizePending && !_resizing && _swapchainExtent.width > 0 && _swapchainExtent.height > 0;
    }
    Frame* WindowRenderer::GetCurrentFrame() const
    {
        return _frames.at(_rendered % _frames.size()).get();
    }
    vk::Extent3D WindowRenderer::GetSwapchainExtent() const
    {
        return _swapchainExtent;
    }
    GraphicsModule* WindowRenderer::GetGraphicsModule() const
    {
        return _module;
    }
    void WindowRenderer::OnDispose()
    {
        DestroyFrames();
        DestroySwapchain();
        _module->GetInstance().destroySurfaceKHR(_surface);
    }
    WindowRenderer::WindowRenderer(GraphicsModule* module, io::Window* window)
    {
        _module = module;
        _window = window;
        _surface = window->CreateSurface(module->GetInstance());
        _viewport.minDepth = 0.0f;
        _viewport.maxDepth = 1.0f;
        _graphImagePool = shared<GraphImagePool>();
    }
}
