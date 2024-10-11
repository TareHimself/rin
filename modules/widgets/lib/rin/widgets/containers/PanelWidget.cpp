#include "rin/widgets/containers/PanelWidget.hpp"
#include "rin/widgets/slots/PanelWidgetSlot.hpp"

bool PanelWidget::NearlyEqual(double a, double b, double tolerance)
{
    return abs(a - b) < tolerance;
}

Vec2<float> PanelWidget::ComputeContentSize()
{
    return Vec2{0.0f};
}

void PanelWidget::ArrangeSlots(const Vec2<float>& drawSize)
{
    for (auto& containerSlot : GetSlots())
    {
        OnChildSlotUpdated(containerSlot.get());
    }
}

Shared<ContainerWidgetSlot> PanelWidget::MakeSlot(const Shared<Widget>& widget)
{
    return newShared<PanelWidgetSlot>(this, widget);
}

void PanelWidget::OnChildResized(Widget* widget)
{
    ContainerWidget::OnChildResized(widget);
    if (auto slot = GetSlot(widget))
    {
        OnChildSlotUpdated(slot.get());
    }
}

void PanelWidget::OnChildSlotUpdated(ContainerWidgetSlot* slot)
{
    ContainerWidget::OnChildSlotUpdated(slot);
    if (auto asPanelSlot = dynamic_cast<PanelWidgetSlot*>(slot))
    {
        auto widget = asPanelSlot->GetWidget();
        auto panelSize = GetSize();
        auto noOffsetX = NearlyEqual(asPanelSlot->minAnchor.x, asPanelSlot->maxAnchor.x);
        auto noOffsetY = NearlyEqual(asPanelSlot->minAnchor.y, asPanelSlot->maxAnchor.y);

        auto widgetSize = widget->GetDesiredSize();

        Vec2<float> wSize{
            asPanelSlot->sizeToContent && noOffsetX ? widgetSize.x : asPanelSlot->size.x,
            asPanelSlot->sizeToContent && noOffsetY ? widgetSize.y : asPanelSlot->size.y
        };

        auto p1 = asPanelSlot->offset;
        auto p2 = p1 + wSize;

        if (noOffsetX)
        {
            auto a = panelSize.x * asPanelSlot->minAnchor.x;
            p1.x += a;
            p2.x += a;
        }
        else
        {
            p1.x = panelSize.x * asPanelSlot->minAnchor.x + p1.x;
            p2.x = panelSize.x * asPanelSlot->maxAnchor.x - p2.x;
        }

        if (noOffsetY)
        {
            auto a = panelSize.y * asPanelSlot->minAnchor.y;
            p1.y += a;
            p2.y += a;
        }
        else
        {
            p1.y = panelSize.y * asPanelSlot->minAnchor.y + p1.y;
            p2.y = panelSize.y * asPanelSlot->maxAnchor.y - p2.y;
        }

        auto dist = p2 - p1;
        dist = dist * asPanelSlot->alignment;
        auto p1Final = p1 - dist;
        auto p2Final = p2 - dist;
        auto sizeFinal = p2Final - p1Final;

        widget->SetOffset(p1Final);
        widget->SetSize(sizeFinal);
    }
}
