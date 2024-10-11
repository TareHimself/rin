#include "rin/widgets/containers/FitterWidget.hpp"

#include "rin/widgets/ContainerWidgetSlot.hpp"

FitMode FitterWidget::GetMode() const
{
    return _mode;
}

void FitterWidget::SetMode(FitMode mode)
{
    auto oldMode = _mode;
    _mode = mode;
    if (mode != oldMode && GetSurface())
    {
        ArrangeSlots(GetContentSize());
    }
}

Vec2<float> FitterWidget::ComputeContentSize()
{
    if (auto slot = GetSlot(0))
    {
        return slot->GetWidget()->GetDesiredSize();
    }

    return Vec2{0.0f};
}

size_t FitterWidget::GetMaxSlots() const
{
    return 1;
}

void FitterWidget::ArrangeSlots(const Vec2<float>& drawSize)
{
    SizeContent(GetContentSize());
}

Vec2<float> FitterWidget::ComputeContainSize(const Vec2<float>& drawSize, const Vec2<float>& widgetSize)
{
    auto aspect = widgetSize.y / widgetSize.x;
    Vec2 scaledWidgetSize{drawSize.x, drawSize.x * aspect};

    if (drawSize.NearlyEquals(scaledWidgetSize, 0.001)) return scaledWidgetSize;

    return scaledWidgetSize.y < drawSize.y ? scaledWidgetSize : Vec2{drawSize.y / aspect, drawSize.y};
}

Vec2<float> FitterWidget::ComputeCoverSize(const Vec2<float>& drawSize, const Vec2<float>& widgetSize)
{
    auto aspect = widgetSize.y / widgetSize.x;
    Vec2 scaledWidgetSize{drawSize.x, drawSize.x * aspect};

    if (drawSize.NearlyEquals(scaledWidgetSize, 0.001)) return scaledWidgetSize;

    return scaledWidgetSize.y < drawSize.y ? Vec2{drawSize.y / aspect, drawSize.y} : scaledWidgetSize;
}

void FitterWidget::SizeContent(const Vec2<float>& size) const
{
    if (auto slot = GetSlot(0))
    {
        auto widget = slot->GetWidget();
        auto widgetDesiredSize = widget->GetDesiredSize();
        auto newDrawSize = widgetDesiredSize;
        if (!widgetDesiredSize.NearlyEquals(size, 0.001))
        {
            switch (GetMode())
            {
            case FitMode::None:
                break;
            case FitMode::Fill:
                {
                    newDrawSize = size;
                }
                break;
            case FitMode::Contain:
                {
                    newDrawSize = ComputeContainSize(size, widgetDesiredSize);
                }
                break;
            case FitMode::Cover:
                {
                    newDrawSize = ComputeCoverSize(size, widgetDesiredSize);
                }
                break;
            }
        }

        if (!widget->GetSize().NearlyEquals(newDrawSize, 0.001)) widget->SetSize(newDrawSize);

        auto halfSelfDrawSize = size / 2.0f;
        auto halfSlotDrawSize = newDrawSize / 2.0f;

        auto diff = halfSelfDrawSize - halfSlotDrawSize;

        if (!widget->GetOffset().NearlyEquals(diff, 0.001f)) widget->SetOffset(diff);
    }
}
