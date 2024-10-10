#pragma once
#include "ListWidget.hpp"
#include "rin/widgets/ContainerWidget.hpp"

class ScrollableWidget : public ListWidget
{
protected:
    void ApplyScroll() const;
    float _scroll = 0.0f;
public:
    ScrollableWidget(const WidgetAxis& axis);

    bool IsScrollable() const;
    float GetMaxScroll() const;
    float GetScroll() const;

    bool OnScroll(const Shared<ScrollEvent>& event) override;
    bool OnCursorDown(const Shared<CursorDownEvent>& event) override;
    TransformInfo ComputeChildTransform(const Shared<Widget>& widget, const TransformInfo& myTransform) override;
};
