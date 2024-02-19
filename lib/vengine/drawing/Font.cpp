#include "vengine/drawing/DrawingSubsystem.hpp"
#include "vengine/widget/types.hpp"

#include <vengine/drawing/Font.hpp>
#include <vengine/Engine.hpp>
#include <vengine/drawing/MaterialBuilder.hpp>
#include <vengine/io/io.hpp>

namespace vengine::drawing {

void Font::Init(DrawingSubsystem * outer) {
  Object<DrawingSubsystem>::Init(outer);
}


void Font::ReadFrom(Buffer &store) {

}

void Font::WriteTo(Buffer &store) {

}

void Font::SetAtlas(const Managed<Texture2D> &atlas) {
  _atlas = atlas;
}

void Font::SetLayout(const FontLayout &layout) {
  _layout = layout;
}

FontLayout Font::GetLayout() const {
  return _layout;
}

Managed<Texture2D> Font::GetAtlas() const {
  return _atlas;
}

void Font::AddGlyph(const Glyph &glyph) {
  _glyphMapping.emplace(glyph.id,_glyphs.size());
  _glyphs.push(glyph);
}

float Font::GetFontScaling(float fontSize) const {
  return fontSize / _layout.size;
}

bool Font::IsUploaded() const {
  return _gpuGlyphs;
}

void Font::Upload() {
  if (!IsUploaded()) {
    auto gpuData = GetOuter()->GetEngine()->GetDrawingSubsystem().Reserve()->GetAllocator().Reserve()->
                                     CreateUniformCpuGpuBuffer(
                                         sizeof(PackedGpuGlyphs), false);
    const auto data = gpuData->GetMappedData<PackedGpuGlyphs>();
    *data = {};
    
    for (auto glyph : _glyphs) {
      data->glyphs[data->numChars] = Pack(glyph);
      if(++data->numChars >= FONT_CHARACTERS_MAX) {
        break;
      }
    }

    _gpuGlyphs = gpuData;
  }
}

PackedGlyph Font::Pack(const Glyph &glyph) const {
  const auto atlasDims = _atlas->GetSize();
  const auto width = static_cast<float>(atlasDims.width);
  const auto height = static_cast<float>(atlasDims.height);
  PackedGlyph gpuChar{};
  gpuChar.uv = glm::vec4{glyph.rect.x / width,glyph.rect.y / height,(glyph.rect.x + glyph.rect.z) / width,(glyph.rect.y + glyph.rect.w) / height};
  gpuChar.extras = {};
  return gpuChar;
}

std::optional<Glyph> Font::GetGlyph(const int id) const {
  if(_glyphMapping.contains(id)) {
    return _glyphs[_glyphMapping.at(id)];
  }

  return {};
}

std::optional<int> Font::GetGlyphIndex(const int id) const {
  if(_glyphMapping.contains(id)) {
    return _glyphMapping.at(id);
  }

  return {};
}

Ref<AllocatedBuffer> Font::GetPackedGlyphs() const {
  return _gpuGlyphs;
}

//
// void Font::DrawText(Text * text,
//                         const String &content,const drawing::Drawer * drawer,
//                     drawing::SimpleFrameData *frameData,
//                     WidgetParentInfo parentInfo) {
//   float offset = 0;
//   const auto fontScale = (static_cast<float>(text->GetFontSize()) / static_cast<
//                             float>(_originalFontSize));
//   for (const auto tChar : content) {
//     auto fChar = _fontChars.find(static_cast<uint32_t>(tChar));
//     if (fChar == _fontChars.end())
//       continue;
//     
//     const auto scaledWidth = static_cast<float>(fChar->second.width) *
//                              fontScale;
//     const auto scaledHeight = static_cast<float>(fChar->second.height) *
//                               fontScale;
//     FontPushConstants fontData{};
//     fontData.extent = glm::vec4{200 + offset,
//                                 200 + static_cast<float>(fChar->second.yOffset)
//                                        * fontScale,
//                                 scaledWidth, scaledHeight};
//     fontData.fontIdx = _fontCharIndices[fChar->first];
//     _material->BindPipeline(frameData->GetRaw());
//     _material->BindSets(frameData->GetRaw());
//     _material->PushConstant(frameData->GetCmd(), "pFont", fontData);
//     frameData->GetCmd()->draw(6, 1, 0, 0);
//     offset += scaledWidth + _characterSpacing + (
//       static_cast<float>(fChar->second.xOffset)
//       * fontScale);
//   }
// }


void Font::BeforeDestroy() {
  Object::BeforeDestroy();
  GetOuter()->GetEngine()->GetDrawingSubsystem().Reserve()->WaitDeviceIdle();
}
}
