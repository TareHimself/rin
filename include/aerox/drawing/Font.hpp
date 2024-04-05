#pragma once

#include "types.hpp"
#include "aerox/drawing/Texture.hpp"
#include "gen/drawing/Font.gen.hpp"

namespace aerox::drawing {
class MaterialInstance;

constexpr int FONT_CHARACTERS_MAX = 256;

struct FontPushConstants {
  glm::vec4 extent{0};
  glm::vec4  info{0};
};

struct PackedGlyph {
  glm::vec4 info;
};

struct PackedGpuGlyphs {
  PackedGlyph glyphs[FONT_CHARACTERS_MAX] = {};
  int numChars = 0;
};

struct Glyph {
  // Normalized x,y,x + width,y + height
  int id;
  int textureIndex;
  glm::fvec2 size;
  glm::fvec2 hBearing;
  float hAdvance;
  glm::fvec2 vBearing;
  float vAdvance;
  glm::fvec2 uv;

  bool HasTexture() const {
    return textureIndex != -1;
  }

  Glyph Scale(float fontSize) const {
    return Glyph{id,textureIndex,size * fontSize,hBearing * fontSize,hAdvance * fontSize,vBearing * fontSize,vAdvance * fontSize,uv};
  }
};

META_TYPE()
class Font : public Object, public assets::LiveAsset,public GpuNative {

  float _ascender = 0.0f;
  float _descender = 0.0f;
  Array<std::shared_ptr<Texture>> _textures;
  Array<Glyph> _glyphs{};
  std::shared_ptr<AllocatedBuffer> _gpuGlyphs;
  std::unordered_map<int,int> _glyphMapping;

public:

  META_BODY()
  
  void SetAscender(float ascender);
  float GetAscender() const;

  void SetDescender(float descender);
  float GetDescender() const;
  void ReadFrom(Buffer &store) override;
  void WriteTo(Buffer &store) override;
  void SetTextures(const Array<std::shared_ptr<Texture>> &textures);

  Array<std::shared_ptr<Texture>> GetTextures() const;

  virtual void AddGlyph(const Glyph& glyph);

  bool IsUploaded() const override;
  
  void Upload() override;
  
  PackedGlyph Pack(const Glyph& glyph) const;
  
  std::optional<Glyph> GetGlyph(int id) const;

  std::optional<int> GetGlyphIndex(int id) const;

  std::weak_ptr<AllocatedBuffer> GetPackedGlyphs() const;

  void OnDestroy() override;

  META_FUNCTION()
  static std::shared_ptr<Font> Construct() { return newObject<Font>(); }
};
}
