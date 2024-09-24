#include "aerox/widgets/containers/WidgetSizer.hpp"

#include "aerox/widgets/WidgetContainerSlot.hpp"

void WidgetSizer::SetWidthOverride(const std::optional<float>& width)
{
    _widthOverride = width;
    CheckSize();
}

void WidgetSizer::SetHeightOverride(const std::optional<float>& height)
{
    _heightOverride = height;
    CheckSize();
}

Vec2<float> WidgetSizer::ComputeDesiredSize()
{
    if(const auto slot = GetSlot(0))
    {
        auto size = slot->GetWidget()->GetDrawSize();
        return Vec2{_widthOverride.value_or(size.x),_heightOverride.value_or(size.y)};
    }
        
    return Vec2{_widthOverride.value_or(0),_heightOverride.value_or(0)};
}

size_t WidgetSizer::GetMaxSlots() const
{
    return 1;
}

void WidgetSizer::ArrangeSlots(const Vec2<float>& drawSize)
{
    if(const auto slot = GetSlot(0))
    {
        auto widget = slot->GetWidget();
        widget->SetRelativeOffset(Vec2{0.0f,0.0f});
        auto padding = GetPadding();
        widget->SetDrawSize(GetDesiredSize() - Vec2{padding.left + padding.right,padding.top + padding.bottom});
    }
}