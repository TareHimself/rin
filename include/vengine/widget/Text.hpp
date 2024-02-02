#pragma once
#include "Font.hpp"
#include "Widget.hpp"

namespace vengine::widget {
class Text : public Widget {
  String _content;
  Pointer<Font>  _font;
  uint32_t _fontSize = 50;
public:
  virtual void SetFont(const Pointer<Font> &font);
  virtual void SetContent(const String &content);
  virtual String GetContent() const;
  virtual WeakPointer<Font> GetFont() const;
  void SetFontSize(uint32_t fontSize);
  uint32_t GetFontSize() const;
  void Init(WidgetManager * outer) override;

  void DrawSelf( drawing::Drawer * drawer, drawing::SimpleFrameData *
                frameData, WidgetParentInfo parentInfo) override;


  void HandleDestroy() override;
};
}
