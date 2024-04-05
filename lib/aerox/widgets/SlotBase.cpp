#include "aerox/widgets/SlotBase.hpp"

namespace aerox::widgets {
SlotBase::SlotBase(const std::shared_ptr<Widget> &widget) {
  _widget = widget;
}

std::weak_ptr<Widget> SlotBase::GetWidget() {
  return _widget;
}
}
