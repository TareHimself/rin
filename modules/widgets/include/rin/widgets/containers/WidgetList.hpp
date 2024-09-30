#pragma once
#include "rin/widgets/WidgetContainer.hpp"


enum class Axis
{
    Horizontal,
    Vertical
};
    
class WidgetList : public WidgetContainer
{
    Axis _axis = Axis::Horizontal;
public:
        
    WidgetList(const Axis& axis = Axis::Horizontal);

    Axis GetAxis() const;
    void SetAxis(const Axis& axis);
        
        

protected:
    Shared<WidgetContainerSlot> MakeSlot(const Shared<Widget>& widget) override;

    void ArrangeSlots(const Vec2<float>& drawSize) override;

    Vec2<float> ComputeContentSize() override;
};
