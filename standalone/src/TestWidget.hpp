#pragma once
#include "aerox/drawing/Font.hpp"
#include "aerox/widgets/Panel.hpp"
#include "aerox/widgets/Row.hpp"
#include "aerox/widgets/Text.hpp"
#include "aerox/widgets/Widget.hpp"
using namespace aerox::widgets;
class TestWidget : public Panel {
  std::shared_ptr<Row> _row;
  std::shared_ptr<aerox::drawing::Font> _font;
  std::shared_ptr<Text> _fpsText;
  std::list<float> _fps;
public:
  void OnInit(aerox::widgets::WidgetSubsystem * ref) override;
  void BindInput(const std::weak_ptr<aerox::window::Window>& window);
  void Tick(float deltaTime) override;
};
