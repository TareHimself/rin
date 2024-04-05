#pragma once
#include "aerox/drawing/Font.hpp"
#include "aerox/widgets/Panel.hpp"
#include "aerox/widgets/Sizer.hpp"
#include "aerox/widgets/Widget.hpp"
using namespace vengine::widgets;
class RootPanel : public Panel {
protected:
  std::shared_ptr<vengine::drawing::Font> _font; 
  std::shared_ptr<Sizer> _sizer;
public:
  uint32_t currentColor = 0;
  void OnInit(WidgetSubsystem *owner) override;
};
