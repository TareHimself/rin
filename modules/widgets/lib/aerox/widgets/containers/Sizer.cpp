#include "aerox/widgets/containers/Sizer.hpp"

#include "aerox/widgets/ContainerSlot.hpp"

namespace aerox::widgets
{
    void Sizer::SetWidthOverride(const std::optional<float>& width)
    {
        _widthOverride = width;
        CheckSize();
    }

    void Sizer::SetHeightOverride(const std::optional<float>& height)
    {
        _heightOverride = height;
        CheckSize();
    }

    Vec2<float> Sizer::ComputeDesiredSize()
    {
        if(const auto slot = GetSlot(0))
        {
            auto size = slot->GetWidget()->GetDrawSize();
            return Vec2{_widthOverride.value_or(size.x),_heightOverride.value_or(size.y)};
        }
        
        return Vec2{_widthOverride.value_or(0),_heightOverride.value_or(0)};
    }

    size_t Sizer::GetMaxSlots() const
    {
        return 1;
    }

    void Sizer::ArrangeSlots(const Vec2<float>& drawSize)
    {
        if(const auto slot = GetSlot(0))
        {
            auto widget = slot->GetWidget();
            widget->SetRelativeOffset(Vec2{0.0f,0.0f});
            auto padding = GetPadding();
            widget->SetDrawSize(GetDesiredSize() - Vec2{padding.left + padding.right,padding.top + padding.bottom});
        }
    }
}
