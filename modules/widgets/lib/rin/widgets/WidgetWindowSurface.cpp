#include "rin/widgets/WidgetWindowSurface.hpp"

#include "rin/widgets/event/CursorDownEvent.hpp"
#include "rin/widgets/event/CursorMoveEvent.hpp"
#include "rin/widgets/event/CursorUpEvent.hpp"
#include "rin/widgets/event/ResizeEvent.hpp"
WidgetWindowSurface::WidgetWindowSurface(WindowRenderer* window)
    {
        _windowRenderer = window;
    }

    Vec2<int> WidgetWindowSurface::GetDrawSize() const
    {
        return _windowRenderer->GetWindow()->GetSize();
    }

    Vec2<float> WidgetWindowSurface::GetCursorPosition() const
    {
        return _windowRenderer->GetWindow()->GetCursorPosition();
    }

    void WidgetWindowSurface::Init()
    {
        WidgetSurface::Init();
        auto thisShared = this->GetSharedDynamic<WidgetWindowSurface>();
        auto window = _windowRenderer->GetWindow();
        _drawHandle = _windowRenderer->onDraw->Add<WidgetWindowSurface>(thisShared,&WidgetWindowSurface::Draw);
        _copyHandle = _windowRenderer->onCopy->Add<WidgetWindowSurface>(thisShared,&WidgetWindowSurface::CopyToSwapchain);
        _resizeHandle = window->onResize->Add<WidgetWindowSurface>(thisShared,&WidgetWindowSurface::OnWindowResize);
        _cursorMoveHandle = window->onCursorMoved->Add<WidgetWindowSurface>(thisShared,&WidgetWindowSurface::OnWindowCursorMove);
        _cursorButtonHandle = window->onCursorButton->Add<WidgetWindowSurface>(thisShared,&WidgetWindowSurface::OnWindowCursorButton);
    }

    void WidgetWindowSurface::HandleDrawSkipped(Frame* frame)
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

    void WidgetWindowSurface::HandleBeforeDraw(Frame* frame)
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

    void WidgetWindowSurface::HandleAfterDraw(Frame* frame)
    {
        if(auto image = GetDrawImage())
        {
            image->Barrier(frame->GetCommandBuffer(),vk::ImageLayout::eColorAttachmentOptimal,vk::ImageLayout::eTransferSrcOptimal);
        }
    }

    void WidgetWindowSurface::CopyToSwapchain(Frame* frame, const vk::Image& dest, const vk::Extent2D& destExtent)
    {
        if(auto drawImage = GetDrawImage())
        {
            drawImage->CopyTo(frame->GetCommandBuffer(),dest,{destExtent.width,destExtent.height,1});
        }
    }

    void WidgetWindowSurface::OnWindowResize(Window * window,const Vec2<int>& size)
    {
        NotifyResize(newShared<ResizeEvent>(this->GetSharedDynamic<WidgetWindowSurface>(),size.Cast<float>()));
    }

    void WidgetWindowSurface::OnWindowCursorMove(Window* window, const Vec2<float>& position)
    {
        NotifyCursorMove(newShared<CursorMoveEvent>(this->GetSharedDynamic<WidgetWindowSurface>(),position));
    }

    void WidgetWindowSurface::OnWindowCursorButton(Window* window,  CursorButton button, InputState state)
    {
        if(state == InputState::Pressed)
        {
            NotifyCursorDown(newShared<CursorDownEvent>(this->GetSharedDynamic<WidgetWindowSurface>(),button,GetCursorPosition()));
        }
        else if(state == InputState::Released)
        {
            NotifyCursorUp(newShared<CursorUpEvent>(this->GetSharedDynamic<WidgetWindowSurface>(),button,GetCursorPosition()));
        }
    }

    void WidgetWindowSurface::OnDispose(bool manual)
    {
        WidgetSurface::OnDispose(manual);
        _drawHandle.UnBind();
        _copyHandle.UnBind();
        _resizeHandle.UnBind();
        _cursorMoveHandle.UnBind();
        _cursorButtonHandle.UnBind();
    }
