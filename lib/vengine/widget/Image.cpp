#include "vengine/drawing/Texture2D.hpp"

#include <vengine/widget/Image.hpp>
#include <vengine/widget/WidgetSubsystem.hpp>
#include <vengine/Engine.hpp>
#include <vengine/drawing/MaterialBuilder.hpp>
#include <vengine/io/io.hpp>

namespace vengine::widget {
void Image::Init(WidgetSubsystem * outer) {
  Widget::Init(outer);
  const auto drawer = outer->GetEngine()->GetDrawingSubsystem().Reserve();
  
  _imageMat = drawing::MaterialBuilder()
  .SetType(drawing::EMaterialType::UI)
  .AddShader(drawing::Shader::FromSource(io::getRawShaderPath("2d/rect.vert")))
  .AddShader(drawing::Shader::FromSource(io::getRawShaderPath("2d/image.frag")))
  .Create();
  _imageMat->SetBuffer<UiGlobalBuffer>("UiGlobalBuffer",outer->GetGlobalBuffer());
}

void Image::SetTexture(const Managed<drawing::Texture2D> &image) {
  _image = image;
  InvalidateCachedSize();
}


Ref<drawing::Texture2D> Image::GetTexture() const {
  return _image;
}

void Image::Draw(drawing::SimpleFrameData *frameData,
                 DrawInfo info) {
  const auto myDrawRect = CalculateFinalRect(info.drawRect);

  const auto material = _imageMat;
  const auto ogFrameData = frameData->GetRaw();
  WidgetPushConstants drawData{};
  
  drawData.extent = myDrawRect;
  drawData.time.x = 0;
  const auto textureToDraw = _image ? _image : GetOuter()->GetEngine()->GetDrawingSubsystem().Reserve()->GetDefaultErrorCheckerboardTexture();
  material->BindPipeline(ogFrameData);
  material->BindSets(ogFrameData);
  material->SetDynamicTexture(frameData->GetRaw(),"ImageT",textureToDraw);
  material->PushConstant(frameData->GetCmd(),"pRect",drawData);
  
  frameData->GetCmd()->draw(6,1,0,0);
}

void Image::BeforeDestroy() {
  Widget::BeforeDestroy();
  _imageMat.Clear();
}

Size2D Image::ComputeDesiredSize() const {
  if(_image) {
    const auto imageSize = _image->GetSize();
    
    return {static_cast<float>(imageSize.width), static_cast<float>(imageSize.height)};
  }

  return {0,0};
}

bool Image::OnMouseDown(
    const std::shared_ptr<window::MouseButtonEvent> &event) {
  GetOuter()->GetLogger()->info("Image clicked");
  return true;
}

void Image::OnMouseEnter(const std::shared_ptr<window::MouseMovedEvent> &event) {
  Widget::OnMouseEnter(event);
  GetOuter()->GetLogger()->info("Image Hover Start");
}

void Image::OnMouseLeave(const std::shared_ptr<window::MouseMovedEvent> &event) {
  Widget::OnMouseLeave(event);
  GetOuter()->GetLogger()->info("Image Hover End");
}
}
