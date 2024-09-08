#pragma once
#include "aerox/core/Disposable.hpp"

namespace aerox::widgets
{
    class Widget;

    class ContainerSlot : public Disposable
    {
        Shared<Widget> _widget{};
    public:
        Shared<Widget> GetWidget() const;
    };
}
