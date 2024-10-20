#pragma once
#include "WCList.hpp"
#include "rin/widgets/ContainerWidget.hpp"

class WCScrollable : public WCList
{
protected:
    void ApplyScroll() const;
    float _scroll = 0.0f;
    float _scrollScale = 5.0f;
public:

    WCScrollable();
    WCScrollable(const WidgetAxis& axis);
    
    bool IsScrollable() const;
    float GetMaxScroll() const;
    float GetScroll() const;
    float GetScrollScale() const;
    void SetScrollScale(float scale);

    bool OnScroll(const Shared<ScrollEvent>& event) override;
    bool OnCursorDown(const Shared<CursorDownEvent>& event) override;
    TransformInfo ComputeChildTransform(const Shared<Widget>& widget, const TransformInfo& myTransform) override;
};
