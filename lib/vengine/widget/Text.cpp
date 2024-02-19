#include "vengine/drawing/Font.hpp"
#include "vengine/drawing/MaterialBuilder.hpp"
#include "vengine/io/io.hpp"
#include "vengine/widget/WidgetSubsystem.hpp"
#include "vengine/widget/utils.hpp"

#include <vengine/widget/Text.hpp>
#include <vengine/Engine.hpp>
#include <vengine/assets/AssetSubsystem.hpp>

namespace vengine::widget {
class Font;

void Text::SetFont(const Managed<drawing::Font> &font) {
  _font = font;
  InvalidateCachedSize();
}

void Text::SetContent(const String &content) {
  _content = content;
  InvalidateCachedSize();
}

String Text::GetContent() const {
  return _content;
}

Ref<drawing::Font> Text::GetFont() const {
  return _font;
}

void Text::SetFontSize(const uint32_t fontSize) {
  _fontSize = fontSize;
  InvalidateCachedSize();
}

uint32_t Text::GetFontSize() const {
  return _fontSize;
}

void Text::Init(WidgetSubsystem * outer) {
  Widget::Init(outer);
  _font = Engine::Get()->GetAssetSubsystem().Reserve()->ImportFont(R"(D:\Github\vengine\NotoSans)");
  _material = drawing::MaterialBuilder()
  .SetType(drawing::EMaterialType::UI)
  .AddShader(drawing::Shader::FromSource(io::getRawShaderPath("2d/font.vert")))
  .AddShader(drawing::Shader::FromSource(io::getRawShaderPath("2d/font.frag")))
  .Create();
}

Size2D Text::GetDesiredSize() {
  if(_font) {
    float width = 0;
    float height = 0;
    for(auto i = 0; i < _content.size(); i++) {
      auto glyph = _font->GetGlyph(_content.at(i));
    
      if(!glyph.has_value()) {
        continue;
      }
      const auto fontScale = _font->GetFontScaling(_fontSize);
      
      width += i == _content.size() - 1 ? glyph->rect.z * fontScale: glyph->advance * fontScale;
      height = std::max(glyph->rect.w * fontScale, height);
    }
    
    return {width,height};
  }

  return {};
}

void Text::Draw(WidgetFrameData *frameData, DrawInfo info) {

  if(!GetDrawRect().HasIntersection(info.clip)) {
    return;
  }
  
  if(!_material || !_font) {
    return;
  }

  if(!_font->IsUploaded()) {
    _font->Upload();
  }

  const auto fontAtlas = _font->GetAtlas();

  if(!fontAtlas) {
    return;
  }

  bindMaterial(frameData,_material);

  _material->SetTexture("AtlasT",fontAtlas);
  _material->SetBuffer("FontChars",_font->GetPackedGlyphs());
  
  const auto drawScale = _font->GetFontScaling(_fontSize);
  
  const auto startPosition = GetDrawRect().GetPoint();
  float xOffset = 0.0f;
  
  for (const auto tChar : _content) {
    auto glyph = _font->GetGlyph(tChar);
    
    if(!glyph.has_value()) {
      continue;
    }

    const auto width = glyph->rect.z * drawScale;
    const auto height = glyph->rect.w * drawScale;

    
    drawing::FontPushConstants fontData{};
    fontData.extent = glm::vec4{startPosition.x + xOffset,
                                startPosition.y + (GetDrawRect().GetSize().height - height),
                                width, height};
    fontData.fontIdx = _font->GetGlyphIndex(tChar).value();

    _material->Push(frameData->GetCmd(), "pFont", fontData);

    frameData->DrawQuad();
    
    xOffset += glyph->advance * drawScale;
  }
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
