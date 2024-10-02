#include "rin/widgets/containers/SizerWidget.hpp"

#include "rin/widgets/ContainerWidgetSlot.hpp"

void SizerWidget::SetWidthOverride(const std::optional<float>& width)
{
    _widthOverride = width;
    CheckSize();
}

void SizerWidget::SetHeightOverride(const std::optional<float>& height)
{
    _heightOverride = height;
    CheckSize();
}

Vec2<float> SizerWidget::ComputeContentSize()
{
    if(const auto slot = GetSlot(0))
    {
        auto size = slot->GetWidget()->GetDesiredSize();
        return Vec2{_widthOverride.value_or(size.x),_heightOverride.value_or(size.y)};
    }
        
    return Vec2{_widthOverride.value_or(0),_heightOverride.value_or(0)};
}

size_t SizerWidget::GetMaxSlots() const
{
    return 1;
}

void SizerWidget::ArrangeSlots(const Vec2<float>& drawSize)
{
    if(const auto slot = GetSlot(0))
    {
        auto widget = slot->GetWidget();
        widget->SetOffset(Vec2{0.0f,0.0f});
        auto padding = GetPadding();
        widget->SetSize(GetDesiredSize() - Vec2{padding.left + padding.right,padding.top + padding.bottom});
    }
}