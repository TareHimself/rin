#pragma once
#include "rin/widgets/ContainerWidget.hpp"


class WCSwitcher : public ContainerWidget
{
    int _selectedIndex = 0;

protected:
    Shared<Widget> NotifyChildrenCursorDown(const Shared<CursorDownEvent>& event,
                                                    const TransformInfo& transform) override;
    bool NotifyChildrenCursorEnter(const Shared<CursorMoveEvent>& event, const TransformInfo& transform,
                                           std::vector<Shared<Widget>>& items) override;
    bool NotifyChildrenCursorMove(const Shared<CursorMoveEvent>& event, const TransformInfo& transform) override;
    bool NotifyChildrenScroll(const Shared<ScrollEvent>& event, const TransformInfo& transform) override;

    void CollectContent(const TransformInfo& transform, WidgetDrawCommands& drawCommands) override;
public:
    int GetSelectedIndex() const;
    void SetSelectedIndex(int index);

    Vec2<float> ComputeContentSize() override;
    void ArrangeSlots(const Vec2<float>& drawSize) override;
    Shared<ContainerWidgetSlot> GetActiveSlot() const;
    Shared<Widget> GetActiveWidget() const;
    
    
};
