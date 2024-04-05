#pragma once
#include "Color.hpp"
#include "Widget.hpp"

namespace aerox::drawing {
class Font;
}

namespace aerox::widgets {
struct GpuTextOptions {
  glm::vec4 color;
};

class Text : public Widget {
  
  String _content;
  std::shared_ptr<drawing::Font>  _font;
  std::shared_ptr<drawing::MaterialInstance> _material;
  std::shared_ptr<drawing::AllocatedBuffer> _gpuInfo;
  float _fontSize = 50;
  float _spacing = 4.0f;
  Color _color = {1.0f};
public:
  virtual void SetFont(const std::shared_ptr<drawing::Font> &font);
  virtual void SetContent(const String &content);
  virtual String GetContent() const;
  virtual std::weak_ptr<drawing::Font> GetFont() const;
  void SetFontSize(float fontSize);
  float GetFontSize() const;
  void OnInit(WidgetSubsystem * ref) override;
  Size2D GetDesiredSize() override;
  void Draw(WidgetFrameData *frameData, DrawInfo info) override;
  virtual void SetColor(const Color& color);
  // void DrawSelf( drawing::Drawer * drawer, drawing::SimpleFrameData *
  //               frameData, DrawInfo parentInfo) override;

  void OnDestroy() override;

  virtual void UpdateOptionsBuffer();
};
}
