#pragma once
#include "Widget.hpp"

namespace vengine {
namespace drawing {
class Font;
}
}

namespace vengine::widget {
class Text : public Widget {
  String _content;
  Managed<drawing::Font>  _font;
  uint32_t _fontSize = 50;
public:
  virtual void SetFont(const Managed<drawing::Font> &font);
  virtual void SetContent(const String &content);
  virtual String GetContent() const;
  virtual Ref<drawing::Font> GetFont() const;
  void SetFontSize(uint32_t fontSize);
  uint32_t GetFontSize() const;
  void Init(WidgetSubsystem * outer) override;

  // void DrawSelf( drawing::Drawer * drawer, drawing::SimpleFrameData *
  //               frameData, DrawInfo parentInfo) override;


  void BeforeDestroy() override;
};
}
