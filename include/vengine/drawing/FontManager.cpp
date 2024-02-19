#include "FontManager.hpp"

#include "vengine/utils.hpp"

namespace vengine::drawing {
void FontManager::Init(DrawingSubsystem *outer) {
  Object<DrawingSubsystem>::Init(outer);
  
  utils::vassert(FT_Init_FreeType(&_library) == FT_Err_Ok,"Failed to load freetype library");
  
}

Ref<Font> FontManager::LoadFont(const fs::path &path) {
  FT_Face face{};
  
  if(const auto error = FT_New_Face(_library,path.string().c_str(),0,&face)) {
    if(error == FT_Err_Unknown_File_Format) {
      utils::vassert(false,"{} is not a font",path.string());
    }

    utils::vassert(false,"failed to load font {}",path.string());
    return {};
  }

  utils::vassert(FT_Set_Char_Size(face,0,16 * 64,300,300) == FT_Err_Ok,"failed to set font size {}",path.string());
  
}
}
