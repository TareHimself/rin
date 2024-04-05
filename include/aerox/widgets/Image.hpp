#pragma once
#include "Color.hpp"
#include "GeometryWidget.hpp"
#include "Widget.hpp"
#include "aerox/drawing/MaterialInstance.hpp"

namespace aerox::widgets {

struct ImageGpuData {
  glm::vec4 tint; // the image tint
  int bHasTexture = 0;
  float borderRadius = 0.0f;
};

class Image : public GeometryWidget {
  std::shared_ptr<drawing::Texture> _image;
  std::shared_ptr<drawing::AllocatedBuffer> _options;
  std::shared_ptr<drawing::MaterialInstance> _imageMat;
  Color _tint = {1.0f};
  
public:
  void OnInit(WidgetSubsystem * ref) override;
  void SetTexture(const std::shared_ptr<drawing::Texture> &image);
  std::weak_ptr<drawing::Texture> GetTexture() const;

  void Draw(WidgetFrameData *frameData, DrawInfo info) override;

  void OnDestroy() override;

  virtual void UpdateOptionsBuffer();

  virtual void SetTint(const Color& tint);

  Size2D ComputeDesiredSize() const override;
};
}
