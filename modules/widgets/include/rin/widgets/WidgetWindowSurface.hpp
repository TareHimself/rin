#pragma once
#include "WidgetSurface.hpp"

class WidgetWindowSurface : public WidgetSurface
{
    WindowRenderer* _windowRenderer = nullptr;
    DelegateListHandle _drawHandle{};
    DelegateListHandle _copyHandle{};
    DelegateListHandle _resizeHandle{};
    DelegateListHandle _cursorMoveHandle{};
    DelegateListHandle _cursorButtonHandle{};

public:
    explicit WidgetWindowSurface(WindowRenderer* window);
    Vec2<int> GetDrawSize() const override;
    Vec2<float> GetCursorPosition() const override;
    void Init() override;
    void HandleDrawSkipped(Frame* frame) override;
    void HandleBeforeDraw(Frame* frame) override;
    void HandleAfterDraw(Frame* frame) override;
    void CopyToSwapchain(Frame* frame, const vk::Image& dest, const vk::Extent2D& destExtent);

    virtual void OnWindowResize(Window* window, const Vec2<int>& size);
    virtual void OnWindowCursorMove(Window* window, const Vec2<float>& position);
    virtual void OnWindowCursorButton(Window* window, CursorButton button, InputState state);

    void OnDispose(bool manual) override;
    //virtual void OnWindowScroll(const Shared<ScrollEvent>& event);
};
