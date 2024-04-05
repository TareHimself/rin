#pragma once
#include "aerox/typedefs.hpp"
namespace aerox::widgets {
class Widget;

class SlotBase {
  std::shared_ptr<Widget> _widget;
public:
  SlotBase(const std::shared_ptr<Widget>& widget);
  std::weak_ptr<Widget> GetWidget();
  
};
}
