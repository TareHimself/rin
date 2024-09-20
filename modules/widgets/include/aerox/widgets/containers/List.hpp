#pragma once
#include "aerox/widgets/Container.hpp"


namespace aerox::widgets
{
    enum class Axis
    {
        Horizontal,
        Vertical
    };
    
    class List : public Container
    {
        Axis _axis = Axis::Horizontal;
    public:
        
        List(const Axis& axis = Axis::Horizontal);

        Axis GetAxis() const;
        void SetAxis(const Axis& axis);
        
        

    protected:
        Shared<ContainerSlot> MakeSlot(const Shared<Widget>& widget) override;

        void ArrangeSlots(const Vec2<float>& drawSize) override;

        Vec2<float> ComputeDesiredSize() override;
    };
}
