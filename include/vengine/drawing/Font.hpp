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

struct GpuFontCharacter {
  glm::vec4 uv;
  glm::vec4 extras;
};

struct GpuPackedFontChars {
  GpuFontCharacter chars[FONT_CHARACTERS_MAX];
  int numChars = 0;
};

struct FontCharacter {
  // Normalized x,y,x + width,y + height
  uint32_t atlasWidth;
  uint32_t atlasHeight;
  int x;
  int y;
  int width;
  int height;
  int xOffset;
  int yOffset;
  int xAdvance;
  int atlasId;

  operator GpuFontCharacter() {
    GpuFontCharacter gpuChar;
    gpuChar.uv = glm::vec4{static_cast<float>(x) / static_cast<float>(
                             atlasWidth),
                           static_cast<float>(x + width) / static_cast<float>(
                             atlasWidth),
                           static_cast<float>(y) / static_cast<float>(
                             atlasHeight),
                           static_cast<float>(y + height) / static_cast<float>(
                             atlasHeight)};
    gpuChar.extras = {xOffset, yOffset,
                      // static_cast<float>(xOffset) / static_cast<float>(
                      //                   atlasWidth),
                      //                 static_cast<float>(yOffset) / static_cast<float>(
                      //                   atlasHeight),
                      xAdvance, atlasId};
    return gpuChar;
  }
};

RCLASS()

class Font : public Object<DrawingSubsystem>, public assets::Asset,
      public GpuNative {
  Array<Managed<Texture2D>> _textures;
  Managed<MaterialInstance> _material;
  Managed<AllocatedBuffer> _gpuFontCharacters;
  std::unordered_map<uint32_t, FontCharacter> _fontChars;
  std::unordered_map<uint32_t, uint32_t> _fontCharIndices;
  uint32_t _lineHeight = 0;

  uint32_t _originalFontSize = 200;
  uint32_t _characterSpacing = 4;

public:
  void Init(drawing::DrawingSubsystem *outer) override;

public:
  void ReadFrom(Buffer &store) override;
  void WriteTo(Buffer &store) override;
  void SetTextures(const Array<Managed<Texture2D>> &textures);
  void SetChars(const std::unordered_map<uint32_t, FontCharacter> &chars,
                const std::unordered_map<uint32_t, uint32_t> &indices);
  void SetMaterial(const Managed<MaterialInstance> &material);
  void SetLineHeight(uint32_t lineHeight);
  std::pair<uint32_t, uint32_t> ComputeTextSize(const String &text,
                                                uint32_t fontSize);

  uint32_t GetLineHeight() const;
  Ref<MaterialInstance> GetMaterial() const;

  bool IsUploaded() const override;
  void Upload() override;
  //
  // virtual void DrawText(Text *text,
  //                       const String &content, const drawing::Drawer *drawer,
  //                       drawing::SimpleFrameData *
  //                       frameData, WidgetParentInfo
  //                       parentInfo);

  void BeforeDestroy() override;

  RFUNCTION()
  static Managed<Font> Construct() { return newManagedObject<Font>(); }
  VENGINE_IMPLEMENT_REFLECTED_INTERFACE(Font)
};

REFLECT_IMPLEMENT(Font)
}
