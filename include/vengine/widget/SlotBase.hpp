#pragma once
#include "vengine/Managed.hpp"
namespace vengine::widget {
class Widget;

class SlotBase {
  Managed<Widget> _widget;
public:
  SlotBase(const Managed<Widget>& widget);
  Ref<Widget> GetWidget();
  
};
}
