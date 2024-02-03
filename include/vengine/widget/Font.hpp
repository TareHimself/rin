#pragma once
#include "WidgetManager.hpp"
#include "types.hpp"
#include "vengine/drawing/Texture.hpp"

namespace vengine::widget {
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
    gpuChar.extras = {xOffset,yOffset,
      // static_cast<float>(xOffset) / static_cast<float>(
      //                   atlasWidth),
      //                 static_cast<float>(yOffset) / static_cast<float>(
      //                   atlasHeight),
                      xAdvance, atlasId};
    return gpuChar;
  }
};

class Font
    : public Object<WidgetManager>, public assets::Asset,
      public drawing::GpuNative {
  Array<Ref<drawing::Texture>> _textures;
  Ref<drawing::MaterialInstance> _material;
  Ref<drawing::AllocatedBuffer> _gpuFontCharacters;
  std::unordered_map<uint32_t, FontCharacter> _fontChars;
  std::unordered_map<uint32_t, uint32_t> _fontCharIndices;
  uint32_t _lineHeight = 0;
  
  uint32_t _originalFontSize = 200;
  uint32_t _characterSpacing = 4;

public:
  void Init(WidgetManager * outer) override;
  void UpdateMaterial() const;

public:
  String GetSerializeId() override;
  void ReadFrom(Buffer &store) override;
  void WriteTo(Buffer &store) override;
  void SetTextures(const Array<Ref<drawing::Texture>> &textures);
  void SetChars(const std::unordered_map<uint32_t, FontCharacter> &chars,
                const std::unordered_map<uint32_t, uint32_t> &indices);
  void SetMaterial(const Ref<drawing::MaterialInstance> &material);
  void SetLineHeight(uint32_t lineHeight);
  std::pair<uint32_t, uint32_t> ComputeTextSize(const String &text,uint32_t fontSize);

  uint32_t GetLineHeight() const;
  WeakRef<drawing::MaterialInstance> GetMaterial() const;

  bool IsUploaded() const override;
  void Upload() override;

  virtual void DrawText(Text * text,
                        const String &content,const drawing::Drawer * drawer,
                        drawing::SimpleFrameData *
                        frameData, WidgetParentInfo
                        parentInfo);
  
  void HandleDestroy() override;
};
}
