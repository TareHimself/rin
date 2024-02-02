#include <vengine/widget/Text.hpp>
#include <vengine/Engine.hpp>
#include <vengine/assets/AssetManager.hpp>

namespace vengine::widget {

void Text::SetFont(const Pointer<Font> &font) {
  _font = font;
}

void Text::SetContent(const String &content) {
  _content = content;
}

String Text::GetContent() const {
  return _content;
}

WeakPointer<Font> Text::GetFont() const {
  return _font;
}

void Text::SetFontSize(const uint32_t fontSize) {
  _fontSize = fontSize;
}

uint32_t Text::GetFontSize() const {
  return _fontSize;
}

void Text::Init(WidgetManager * outer) {
  Widget::Init(outer);
  _font = GetOuter()->GetEngine()->GetAssetManager().Reserve()->ImportFont(R"(D:\Github\vengine\NotoSans)");
}

void Text::DrawSelf(drawing::Drawer * drawer,
                    drawing::SimpleFrameData *frameData, WidgetParentInfo parentInfo) {
  if(!_font->IsUploaded()) {
    _font->Upload();
  }
  
  _font->DrawText(this,_content,drawer, frameData, parentInfo);
}

void Text::HandleDestroy() {
  Widget::HandleDestroy();
  _font.Clear();
}
}
