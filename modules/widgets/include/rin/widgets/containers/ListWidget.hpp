#pragma once
#include "rin/widgets/ContainerWidget.hpp"
#include "rin/widgets/WidgetAxis.hpp"


class ListWidget : public ContainerWidget
{
    WidgetAxis _axis = WidgetAxis::Horizontal;
public:
        
    ListWidget(const WidgetAxis& axis = WidgetAxis::Horizontal);

    WidgetAxis GetAxis() const;
    void SetAxis(const WidgetAxis& axis);
        
        

protected:
    void ArrangeSlots(const Vec2<float>& drawSize) override;

    Vec2<float> ComputeContentSize() override;
};
