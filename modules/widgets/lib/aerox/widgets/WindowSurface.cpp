#include "aerox/widgets/WindowSurface.hpp"

#include "aerox/widgets/event/CursorDownEvent.hpp"
#include "aerox/widgets/event/CursorMoveEvent.hpp"
#include "aerox/widgets/event/CursorUpEvent.hpp"
#include "aerox/widgets/event/ResizeEvent.hpp"
namespace aerox::widgets
{
    WindowSurface::WindowSurface(graphics::WindowRenderer* window)
    {
        _windowRenderer = window;
    }

    Vec2<int> WindowSurface::GetDrawSize() const
    {
        return _windowRenderer->GetWindow()->GetSize();
    }

    Vec2<float> WindowSurface::GetCursorPosition() const
    {
        return _windowRenderer->GetWindow()->GetCursorPosition();
    }

    void WindowSurface::Init()
    {
        Surface::Init();
        auto thisShared = this->GetSharedDynamic<WindowSurface>();
        auto window = _windowRenderer->GetWindow();
        _drawHandle = _windowRenderer->onDraw->Add<WindowSurface>(thisShared,&WindowSurface::Draw);
        _copyHandle = _windowRenderer->onCopy->Add<WindowSurface>(thisShared,&WindowSurface::CopyToSwapchain);
        _resizeHandle = window->onResize->Add<WindowSurface>(thisShared,&WindowSurface::OnWindowResize);
        _cursorMoveHandle = window->onCursorMoved->Add<WindowSurface>(thisShared,&WindowSurface::OnWindowCursorMove);
        _cursorButtonHandle = window->onCursorButton->Add<WindowSurface>(thisShared,&WindowSurface::OnWindowCursorButton);
    }

    void WindowSurface::HandleDrawSkipped(graphics::Frame* frame)
    {
        if(auto image = GetDrawImage())
        {
            image->Barrier(frame->GetCommandBuffer(),vk::ImageLayout::eUndefined,vk::ImageLayout::eTransferSrcOptimal);
        }

        if(auto image = GetCopyImage())
        {
            image->Barrier(frame->GetCommandBuffer(),vk::ImageLayout::eUndefined,vk::ImageLayout::eTransferSrcOptimal);
        }
    }

    void WindowSurface::HandleBeforeDraw(graphics::Frame* frame)
    {
        if(auto image = GetDrawImage())
        {
            image->Barrier(frame->GetCommandBuffer(),vk::ImageLayout::eUndefined,vk::ImageLayout::eColorAttachmentOptimal);
        }

        if(auto image = GetCopyImage())
        {
            image->Barrier(frame->GetCommandBuffer(),vk::ImageLayout::eUndefined,vk::ImageLayout::eColorAttachmentOptimal);
        }
    }

    void WindowSurface::HandleAfterDraw(graphics::Frame* frame)
    {
        if(auto image = GetDrawImage())
        {
            image->Barrier(frame->GetCommandBuffer(),vk::ImageLayout::eColorAttachmentOptimal,vk::ImageLayout::eTransferSrcOptimal);
        }
    }

    void WindowSurface::CopyToSwapchain(graphics::Frame* frame, const vk::Image& dest, const vk::Extent2D& destExtent)
    {
        if(auto drawImage = GetDrawImage())
        {
            drawImage->CopyTo(frame->GetCommandBuffer(),dest,{destExtent.width,destExtent.height,1});
        }
    }

    void WindowSurface::OnWindowResize(window::Window * window,const Vec2<int>& size)
    {
        NotifyResize(newShared<ResizeEvent>(this->GetSharedDynamic<WindowSurface>(),size.Cast<float>()));
    }

    void WindowSurface::OnWindowCursorMove(window::Window* window, const Vec2<float>& position)
    {
        NotifyCursorMove(newShared<CursorMoveEvent>(this->GetSharedDynamic<WindowSurface>(),position));
    }

    void WindowSurface::OnWindowCursorButton(window::Window* window,  window::CursorButton button, window::InputState state)
    {
        if(state == window::InputState::Pressed)
        {
            NotifyCursorDown(newShared<CursorDownEvent>(this->GetSharedDynamic<WindowSurface>(),button,GetCursorPosition()));
        }
        else if(state == window::InputState::Released)
        {
            NotifyCursorUp(newShared<CursorUpEvent>(this->GetSharedDynamic<WindowSurface>(),button,GetCursorPosition()));
        }
    }

    void WindowSurface::OnDispose(bool manual)
    {
        Surface::OnDispose(manual);
        _drawHandle.UnBind();
        _copyHandle.UnBind();
        _resizeHandle.UnBind();
        _cursorMoveHandle.UnBind();
        _cursorButtonHandle.UnBind();
    }
}
