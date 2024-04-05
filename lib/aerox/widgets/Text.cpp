#include "aerox/drawing/Font.hpp"
#include "aerox/drawing/MaterialBuilder.hpp"
#include "aerox/io/io.hpp"
#include "aerox/widgets/WidgetSubsystem.hpp"
#include "aerox/widgets/utils.hpp"

#include <aerox/widgets/Text.hpp>
#include <aerox/Engine.hpp>
#include <aerox/assets/AssetSubsystem.hpp>

namespace aerox::widgets {

void Text::SetFont(const std::shared_ptr<drawing::Font> &font) {
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

std::weak_ptr<drawing::Font> Text::GetFont() const {
  return _font;
}

void Text::SetFontSize(const float fontSize) {
  _fontSize = fontSize;
  InvalidateCachedSize();
}

float Text::GetFontSize() const {
  return _fontSize;
}

void Text::OnInit(WidgetSubsystem *ref) {
  Widget::OnInit(ref);
  _material = GetOwner()->CreateMaterialInstance({
      drawing::Shader::FromSource(io::getRawShaderPath("2d/font.vert")),
      drawing::Shader::FromSource(io::getRawShaderPath("2d/font.frag"))
  });

  _gpuInfo = GetOwner()->GetOwner()->GetDrawingSubsystem().lock()->
                         GetAllocator().lock()->CreateUniformCpuGpuBuffer<
                           GpuTextOptions>(true);
  _material->SetBuffer("options", _gpuInfo);

  UpdateOptionsBuffer();
}


Size2D Text::GetDesiredSize() {
  if (_font) {
    float width = 0;
    float height = 0;
    for (auto i = 0; i < _content.size(); i++) {
      auto glyph = _font->GetGlyph(_content.at(i));

      if (!glyph.has_value()) {
        continue;
      }

      auto scaledGlyph = glyph->Scale(_fontSize);

      width += scaledGlyph.hAdvance;
      height = std::max(
          std::abs(_font->GetAscender() * _fontSize) + std::abs(
              _font->GetDescender() * _fontSize), height);
    }

    return {width, height};
  }

  return {};
}

void Text::Draw(WidgetFrameData *frameData, DrawInfo info) {

  if (!GetDrawRect().HasIntersection(info.clip)) {
    return;
  }

  if (!_material || !_font) {
    return;
  }

  if (!_font->IsUploaded()) {
    _font->Upload();
    return;
  }

  bindMaterial(frameData, _material);

  _material->SetTextureArray("AtlasT", _font->GetTextures());
  _material->SetBuffer("FontChars", _font->GetPackedGlyphs().lock());

  const auto startPosition = GetDrawRect().GetPoint();
  float xOffset = 0.0f;

  auto baseline = startPosition.y + (_font->GetAscender() * _fontSize);
  std::string toDraw = _content;
  for (auto i = 0; i < toDraw.size(); i++) {
    auto glyph = _font->GetGlyph(toDraw.at(i));

    if (!glyph.has_value()) {
      continue;
    }

    auto scaledGlyph = glyph->Scale(_fontSize);

    const auto width = scaledGlyph.size.x;
    const auto height = scaledGlyph.size.y;

    auto drawYStart = baseline - scaledGlyph.hBearing.y;
    auto drawXStart = startPosition.x + xOffset + scaledGlyph.hBearing.x;

    drawing::FontPushConstants fontData{};
    fontData.extent = glm::vec4{drawXStart,
                                drawYStart,
                                width, height};
    fontData.info.x = static_cast<float>(_font->GetGlyphIndex(toDraw.at(i)).
                                                value());
    fontData.info.y = _fontSize;

    _material->Push(frameData->GetCmd(), "pFont", fontData);

    frameData->DrawQuad();

    xOffset += scaledGlyph.hAdvance;

    //break;
  }
}

void Text::SetColor(const Color &color) {
  _color = color;
  UpdateOptionsBuffer();
}

void Text::OnDestroy() {
  Widget::OnDestroy();
  _font.reset();
}

void Text::UpdateOptionsBuffer() {
  GpuTextOptions opts{_color};
  _gpuInfo->Write(opts);
}
}
