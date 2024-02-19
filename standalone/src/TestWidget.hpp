#pragma once
#include "vengine/widget/Panel.hpp"
#include "vengine/widget/Row.hpp"
#include "vengine/widget/Widget.hpp"
using namespace vengine::widget;
class TestWidget : public Panel {
  vengine::Managed<Row> _row;
public:
  void Init(WidgetSubsystem *outer) override;
  void BindInput(const vengine::Ref<vengine::window::Window>& window);
};
