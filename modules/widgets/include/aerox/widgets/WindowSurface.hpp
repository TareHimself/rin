#pragma once
#include "Surface.hpp"

namespace aerox::widgets
{
    class WindowSurface : public Surface
    {
        graphics::WindowRenderer * _windowRenderer = nullptr;
        DelegateListHandle _drawHandle{};
        DelegateListHandle _copyHandle{};
        DelegateListHandle _resizeHandle{};
        DelegateListHandle _cursorMoveHandle{};
        DelegateListHandle _cursorButtonHandle{};
    public:
        explicit  WindowSurface(graphics::WindowRenderer* window);
        Vec2<int> GetDrawSize() const override;
        Vec2<float> GetCursorPosition() const override;
        void Init() override;
        void HandleDrawSkipped(graphics::Frame* frame) override;
        void HandleBeforeDraw(graphics::Frame* frame) override;
        void HandleAfterDraw(graphics::Frame* frame) override;
        void CopyToSwapchain(graphics::Frame* frame, const vk::Image& dest, const vk::Extent2D& destExtent);

        virtual void OnWindowResize(window::Window * window,const Vec2<int>& size);
        virtual void OnWindowCursorMove(window::Window * window,const Vec2<float>& position);
        virtual void OnWindowCursorButton(window::Window * window, window::CursorButton button, window::InputState state);

        void OnDispose(bool manual) override;
        //virtual void OnWindowScroll(const Shared<ScrollEvent>& event);
    };
}
