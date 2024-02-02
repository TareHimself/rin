#pragma once
#include "Widget.hpp"
#include "vengine/drawing/MaterialInstance.hpp"

namespace vengine::widget {
class Image : public Widget{
  Pointer<drawing::Texture> _image;
  
  Pointer<drawing::MaterialInstance> _imageMat;
public:
  void Init(WidgetManager * outer) override;
  void SetTexture(const Pointer<drawing::Texture> &image);
  WeakPointer<drawing::Texture> GetTexture() const;
  
  void DrawSelf(drawing::Drawer *drawer, drawing::SimpleFrameData *frameData, WidgetParentInfo
                parentInfo) override;

  void HandleDestroy() override;
};
}
