#include "aerox/widgets/slots/PanelSlot.hpp"

#include "aerox/widgets/containers/Panel.hpp"

namespace aerox::widgets
{
    PanelSlot::PanelSlot(Panel * panel,const Shared<Widget>& widget) : ContainerSlot(widget)
    {
        _panel = panel;
    }

    bool PanelSlot::NearlyEqual(double a, double b, double tolerance)
    {
        return abs(a - b) < tolerance;
    }

    void PanelSlot::ComputeSizeAndOffset() const
    {
        auto panelSize = _panel->GetDrawSize();
        auto noOffsetX = NearlyEqual(minAnchor.x,maxAnchor.x);
        auto noOffsetY = NearlyEqual(minAnchor.y,maxAnchor.y);

        auto widgetSize = GetWidget()->GetDesiredSize();

        Vec2<float> wSize{sizeToContent && noOffsetX ? widgetSize.x : size.x,sizeToContent && noOffsetY ? widgetSize.y : size.y};

        auto p1 = offset;
        auto p2 = p1 + wSize;

        if(noOffsetX)
        {
            auto a = panelSize.x * minAnchor.x;
            p1.x += a;
            p2.x += a;
        }
        else
        {
            p1.x = panelSize.x * minAnchor.x + p1.x;
            p2.x = panelSize.x * maxAnchor.x - p2.x;
        }

        if(noOffsetY)
        {
            auto a = panelSize.y * minAnchor.y;
            p1.y += a;
            p2.y += a; 
        }
        else
        {
            p1.y = panelSize.y * minAnchor.y + p1.y;
            p2.y = panelSize.y * maxAnchor.y - p2.y;
        }

        auto dist = p2 - p1;
        dist = dist * alignment;
        auto p1Final = p1 - dist;
        auto p2Final = p2 - dist;
        auto sizeFinal = p2Final - p1Final;

        GetWidget()->SetRelativeOffset(p1Final);
        GetWidget()->SetDrawSize(sizeFinal);
    }
}
