#pragma once
#include "freetype2/ft2build.h"
#include FT_FREETYPE_H
#include "vengine/Object.hpp"
#include <vengine/fs.hpp>

namespace vengine {
namespace drawing {
class Font;
}
}

namespace vengine::drawing {
class DrawingSubsystem;


struct FontFace {
private:
  FT_Face _face{};

public:
  
};
class FontManager : public Object<DrawingSubsystem> {
  FT_Library _library{};
public:
  void Init(DrawingSubsystem *outer) override;


  Ref<Font> LoadFont(const fs::path& path);
};

}
