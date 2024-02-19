#pragma once

#include "types.hpp"
#include "vengine/drawing/Texture2D.hpp"
#include "generated/drawing/Font.reflect.hpp"

namespace vengine {
namespace drawing {
class MaterialInstance;
}
}

namespace vengine::drawing {
class Text;
constexpr int FONT_CHARACTERS_MAX = 256;

struct FontPushConstants {
  glm::vec4 extent{0};
  int fontIdx;
};

struct PackedGlyph {
  glm::vec4 uv;
  glm::vec4 extras;
};

struct PackedGpuGlyphs {
  PackedGlyph glyphs[FONT_CHARACTERS_MAX] = {};
  int numChars = 0;
};


struct FontLayout {
  float size = 0.0f;
  float lineHeight = 0.0f;
  float ascender = 0.0f;
  float descender = 0.0f;
  float underline = 0.0f;
  float underlineThickness = 0.0f;
};

struct Glyph {
  // Normalized x,y,x + width,y + height
  int id;
  
  float advance = 0.0f;
  glm::vec4 rect{0.0f};

  
};

RCLASS()
class Font : public Object<DrawingSubsystem>, public assets::Asset,
      public GpuNative {

  FontLayout _layout{};
  Managed<Texture2D> _atlas;
  Array<Glyph> _glyphs{};
  Managed<AllocatedBuffer> _gpuGlyphs;
  std::unordered_map<int,int> _glyphMapping;
  float _characterSpacing = 4.0f;

public:
  void Init(drawing::DrawingSubsystem *outer) override;
  void ReadFrom(Buffer &store) override;
  void WriteTo(Buffer &store) override;
  void SetAtlas(const Managed<Texture2D> &atlas);

  void SetLayout(const FontLayout& layout);
  FontLayout GetLayout() const;

  Managed<Texture2D> GetAtlas() const;
  
  virtual void AddGlyph(const Glyph& glyph);

  float GetFontScaling(float fontSize) const;
  
  bool IsUploaded() const override;
  
  void Upload() override;
  
  PackedGlyph Pack(const Glyph& glyph) const;
  
  std::optional<Glyph> GetGlyph(int id) const;

  std::optional<int> GetGlyphIndex(int id) const;

  Ref<AllocatedBuffer> GetPackedGlyphs() const;

  void BeforeDestroy() override;

  RFUNCTION()
  static Managed<Font> Construct() { return newManagedObject<Font>(); }
  VENGINE_IMPLEMENT_REFLECTED_INTERFACE(Font)
};

REFLECT_IMPLEMENT(Font)
}
