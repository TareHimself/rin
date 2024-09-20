#pragma once
#include "List.hpp"
#include "aerox/widgets/Container.hpp"

namespace aerox::widgets
{
    class ScrollList : public List
    {
    protected:
        Shared<List> _list{};
        Vec2<float> ComputeDesiredSize() override;
        void ArrangeSlots(const Vec2<float>& drawSize) override;
        Shared<ContainerSlot> MakeSlot(const Shared<Widget>& widget) override;
        void ApplyScroll() const;
        float _scroll = 0.0f;
    public:
        ScrollList(const Axis& axis);

        bool IsScrollable() const;
        float GetMaxScroll() const;
        float GetScroll() const;

        bool OnScroll(const Shared<ScrollEvent>& event) override;
        bool OnCursorDown(const Shared<CursorDownEvent>& event) override;

        void Collect(const TransformInfo& transform, std::vector<Shared<DrawCommand>>& drawCommands) override;

        TransformInfo ComputeChildTransform(const Shared<Widget>& widget, const TransformInfo& myTransform) override;
    };
}
