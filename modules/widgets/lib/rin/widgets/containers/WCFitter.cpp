#include "rin/widgets/containers/WCFitter.hpp"

#include "rin/widgets/ContainerWidgetSlot.hpp"

FitMode WCFitter::GetMode() const
{
    return _mode;
}

void WCFitter::SetMode(const FitMode mode)
{
    const auto oldMode = _mode;
    _mode = mode;
    if (mode != oldMode && GetSurface())
    {
        ArrangeSlots(GetContentSize());
    }
}

Vec2<float> WCFitter::ComputeContentSize()
{
    if (const auto slot = GetSlot(0))
    {
        return slot->GetWidget()->GetDesiredSize();
    }

    return Vec2{0.0f};
}

size_t WCFitter::GetMaxSlots() const
{
    return 1;
}

void WCFitter::ArrangeSlots(const Vec2<float>& drawSize)
{
    SizeContent(GetContentSize());
}

Vec2<float> WCFitter::ComputeContainSize(const Vec2<float>& drawSize, const Vec2<float>& desiredSize)
{
    const auto aspect = desiredSize.y / desiredSize.x;
    const Vec2 scaledWidgetSize{drawSize.x, drawSize.x * aspect};

    if (drawSize.NearlyEquals(scaledWidgetSize, 0.001)) return scaledWidgetSize;

    return scaledWidgetSize.y < drawSize.y ? scaledWidgetSize : Vec2{drawSize.y / aspect, drawSize.y};
}

Vec2<float> WCFitter::ComputeCoverSize(const Vec2<float>& drawSize, const Vec2<float>& desiredSize)
{
    const auto aspect = desiredSize.y / desiredSize.x;
    const Vec2 scaledWidgetSize{drawSize.x, drawSize.x * aspect};

    if (drawSize.NearlyEquals(scaledWidgetSize, 0.001)) return scaledWidgetSize;

    return scaledWidgetSize.y < drawSize.y ? Vec2{drawSize.y / aspect, drawSize.y} : scaledWidgetSize;
}

void WCFitter::SizeContent(const Vec2<float>& size) const
{
    if (const auto slot = GetSlot(0))
    {
        const auto widget = slot->GetWidget();
        const auto widgetDesiredSize = widget->GetDesiredSize();
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

        const auto halfSelfDrawSize = size / 2.0f;
        const auto halfSlotDrawSize = newDrawSize / 2.0f;

        if (const auto diff = halfSelfDrawSize - halfSlotDrawSize; !widget->GetOffset().NearlyEquals(diff, 0.001f))
        {
            widget->SetOffset(diff);
        }
    }
}
