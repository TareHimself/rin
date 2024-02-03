#pragma once
#include "Widget.hpp"
#include "vengine/drawing/MaterialInstance.hpp"

namespace vengine::widget {
class Image : public Widget{
  Ref<drawing::Texture> _image;
  
  Ref<drawing::MaterialInstance> _imageMat;
public:
  void Init(WidgetManager * outer) override;
  void SetTexture(const Ref<drawing::Texture> &image);
  WeakRef<drawing::Texture> GetTexture() const;
  
  void DrawSelf(drawing::Drawer *drawer, drawing::SimpleFrameData *frameData, WidgetParentInfo
                parentInfo) override;

  void HandleDestroy() override;
};
}
