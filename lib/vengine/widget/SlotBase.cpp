#include "vengine/widget/SlotBase.hpp"

namespace vengine::widget {
SlotBase::SlotBase(const Managed<Widget> &widget) {
  _widget = widget;
}

Ref<Widget> SlotBase::GetWidget() {
  return _widget;
}
}
