#include "vengine/drawing/Font.hpp"
#include "vengine/widget/WidgetSubsystem.hpp"

#include <vengine/widget/Text.hpp>
#include <vengine/Engine.hpp>
#include <vengine/assets/AssetSubsystem.hpp>

namespace vengine::widget {
class Font;

void Text::SetFont(const Managed<drawing::Font> &font) {
  _font = font;
}

void Text::SetContent(const String &content) {
  _content = content;
}

String Text::GetContent() const {
  return _content;
}

Ref<drawing::Font> Text::GetFont() const {
  return _font;
}

void Text::SetFontSize(const uint32_t fontSize) {
  _fontSize = fontSize;
}

uint32_t Text::GetFontSize() const {
  return _fontSize;
}

void Text::Init(WidgetSubsystem * outer) {
  Widget::Init(outer);
  _font = GetOuter()->GetEngine()->GetAssetSubsystem().Reserve()->ImportFont(R"(D:\Github\vengine\NotoSans)");
}

// void Text::DrawSelf(drawing::Drawer * drawer,
//                     drawing::SimpleFrameData *frameData, DrawInfo parentInfo) {
//   if(_font) {
//     if(!_font->IsUploaded()) {
//       _font->Upload();
//     }
//   
//     //_font->DrawText(this,_content,drawer, frameData, parentInfo);
//   }
// }

void Text::BeforeDestroy() {
  Widget::BeforeDestroy();
  _font.Clear();
}
}
