#include "rin/widgets/containers/WCSizer.hpp"

#include "rin/widgets/ContainerWidgetSlot.hpp"

void WCSizer::SetWidthOverride(const std::optional<float>& width)
{
    _widthOverride = width;
    CheckSize();
}

void WCSizer::SetHeightOverride(const std::optional<float>& height)
{
    _heightOverride = height;
    CheckSize();
}

void WCSizer::SetOverrides(const std::optional<Vec2<float>>& overrides)
{
    if(overrides.has_value())
    {
        _heightOverride = overrides->y;
        _widthOverride = overrides->x;
    }
    else
    {
        _heightOverride = {};
        _widthOverride = {};
    }
    CheckSize();
}

Vec2<float> WCSizer::ComputeContentSize()
{
    if (const auto slot = GetSlot(0))
    {
        auto size = slot->GetWidget()->GetDesiredSize();
        return Vec2{_widthOverride.value_or(size.x), _heightOverride.value_or(size.y)};
    }

    return Vec2{_widthOverride.value_or(0), _heightOverride.value_or(0)};
}

size_t WCSizer::GetMaxSlots() const
{
    return 1;
}

void WCSizer::ArrangeSlots(const Vec2<float>& drawSize)
{
    if (const auto slot = GetSlot(0))
    {
        auto widget = slot->GetWidget();
        widget->SetOffset(Vec2{0.0f, 0.0f});
        auto padding = GetPadding();
        widget->SetSize(GetDesiredSize() - Vec2{padding.left + padding.right, padding.top + padding.bottom});
    }
}
