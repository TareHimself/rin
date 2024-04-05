#include "aerox/drawing/DrawingSubsystem.hpp"
#include "aerox/widgets/types.hpp"

#include <aerox/drawing/Font.hpp>
#include <aerox/Engine.hpp>
#include <aerox/drawing/MaterialBuilder.hpp>
#include <aerox/io/io.hpp>

namespace aerox::drawing {

void Font::SetAscender(float ascender) {
  _ascender = ascender;
}

float Font::GetAscender() const {
  return _ascender;
}

void Font::SetDescender(const float descender) {
  _descender = descender;
}

float Font::GetDescender() const {
  return _descender;
}

void Font::ReadFrom(Buffer &store) {

}

void Font::WriteTo(Buffer &store) {

}

void Font::SetTextures(const Array<std::shared_ptr<Texture>> &textures) {
  _textures = textures;
}

Array<std::shared_ptr<Texture>> Font::GetTextures() const {
  return _textures;
}

void Font::AddGlyph(const Glyph &glyph) {
  _glyphMapping.emplace(glyph.id, _glyphs.size());
  _glyphs.push(glyph);
}

bool Font::IsUploaded() const {
  return static_cast<bool>(_gpuGlyphs);
}

void Font::Upload() {
  if (!IsUploaded()) {
    auto gpuData = Engine::Get()->
                   GetDrawingSubsystem().lock()->GetAllocator().lock()->
                   CreateUniformCpuGpuBuffer(
                       sizeof(PackedGpuGlyphs), false);

    PackedGpuGlyphs packed;
    for (auto glyph : _glyphs) {
      packed.glyphs[packed.numChars] = Pack(glyph);
      if (++packed.numChars >= FONT_CHARACTERS_MAX) {
        break;
      }
    }

    gpuData->Write(packed);
    _gpuGlyphs = gpuData;
  }
}

PackedGlyph Font::Pack(const Glyph &glyph) const {
  PackedGlyph gpuChar{};
  gpuChar.info = glm::vec4{glyph.textureIndex, 0, 0, 0};
  return gpuChar;
}

std::optional<Glyph> Font::GetGlyph(const int id) const {
  if (_glyphMapping.contains(id)) {
    return _glyphs[_glyphMapping.at(id)];
  }

  return {};
}

std::optional<int> Font::GetGlyphIndex(const int id) const {
  if (_glyphMapping.contains(id)) {
    return _glyphMapping.at(id);
  }

  return {};
}

std::weak_ptr<AllocatedBuffer> Font::GetPackedGlyphs() const {
  return _gpuGlyphs;
}


void Font::OnDestroy() {
  Object::OnDestroy();
  Engine::Get()->GetDrawingSubsystem().lock()->WaitDeviceIdle();
}
}
