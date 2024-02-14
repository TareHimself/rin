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

void Font::SetTextures(const Array<Managed<drawing::Texture2D>> &textures) {
  _textures = textures;
}

void Font::SetChars(const std::unordered_map<uint32_t, FontCharacter> &chars,
                    const std::unordered_map<uint32_t, uint32_t> &indices) {
  _fontChars = chars;
  _fontCharIndices = indices;
}

void Font::SetMaterial(const Managed<drawing::MaterialInstance> &material) {
  _material = material;
}

void Font::SetLineHeight(uint32_t lineHeight) {
  _lineHeight = lineHeight;
}

std::pair<uint32_t, uint32_t> Font::ComputeTextSize(const String &text,
                                                    uint32_t fontSize) {
  float width = 0;
  float height = 0;
  const auto fontScale = (static_cast<float>(fontSize) / static_cast<float>(
                            _originalFontSize));

  for (const auto tChar : text) {
    auto fChar = _fontChars.find(static_cast<uint32_t>(tChar));
    if (fChar == _fontChars.end())
      continue;
    const auto scaledWidth = fChar->second.width + fChar->second.xOffset;
    const auto scaledHeight = fChar->second.height;
    width += fChar->second.xOffset + scaledWidth + _characterSpacing;
    height = std::max(static_cast<float>(scaledHeight), height);
  }

  width -= _characterSpacing;

  return {static_cast<uint32_t>(width * fontScale), static_cast<uint32_t>(height * fontScale)};
}

uint32_t Font::GetLineHeight() const {
  return _lineHeight;
}

Ref<drawing::MaterialInstance> Font::GetMaterial() const {
  return _material;
}

bool Font::IsUploaded() const {
  return _gpuFontCharacters;
}

void Font::Upload() {
  if (!IsUploaded()) {
    _gpuFontCharacters = GetOuter()->GetEngine()->GetDrawingSubsystem().Reserve()->GetAllocator().Reserve()->
                                     CreateUniformCpuGpuBuffer(
                                         sizeof(GpuPackedFontChars), false);
    const auto data = static_cast<GpuPackedFontChars *>(_gpuFontCharacters->GetMappedData());
    GpuPackedFontChars packChars{};
    _fontCharIndices.clear();

    for (auto [charId, charData] : _fontChars) {
      packChars.chars[packChars.numChars] = charData;
      _fontCharIndices.emplace(charId, packChars.numChars);
      if (++packChars.numChars >= FONT_CHARACTERS_MAX) {
        break;
      }
    }

    *data = packChars;
  }
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
  _material.Clear();
  _textures.clear();
  _gpuFontCharacters.Clear();
}
}
