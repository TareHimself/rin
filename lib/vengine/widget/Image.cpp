#include "vengine/drawing/Texture2D.hpp"
#include "vengine/widget/utils.hpp"

#include <vengine/widget/Image.hpp>
#include <vengine/widget/WidgetSubsystem.hpp>
#include <vengine/Engine.hpp>
#include <vengine/drawing/MaterialBuilder.hpp>
#include <vengine/io/io.hpp>

namespace vengine::widget {
void Image::Init(WidgetSubsystem *outer) {
  GeometryWidget::Init(outer);
  const auto drawer = Engine::Get()->GetDrawingSubsystem().Reserve();

  _imageMat = GetOuter()->CreateMaterialInstance(
  {drawing::Shader::FromSource(io::getRawShaderPath("2d/rect.vert")),
   drawing::Shader::FromSource(io::getRawShaderPath("2d/image.frag"))});

}

void Image::SetTexture(const Managed<drawing::Texture2D> &image) {
  _image = image;
  if (_imageMat) {
    _imageMat->SetTexture("ImageT", _image);
  }
  InvalidateCachedSize();
}


Ref<drawing::Texture2D> Image::GetTexture() const {
  return _image;
}

void Image::Draw(WidgetFrameData *frameData,
                 DrawInfo info) {

  if (!GetDrawRect().HasIntersection(info.clip)) {
    return;
  }

  auto material = _imageMat;
  const auto ogFrameData = frameData->GetRaw();
  WidgetPushConstants drawData{};

  drawData.clip = info.clip;
  drawData.extent = GetDrawRect();
  bindMaterial(frameData, material);
  material->Push(frameData->GetCmd(), "pRect", drawData);

  frameData->DrawQuad();
}

void Image::BeforeDestroy() {
  Widget::BeforeDestroy();
  _imageMat.Clear();
}

Size2D Image::ComputeDesiredSize() const {
  if (_image) {
    const auto imageSize = _image->GetSize();

    return {static_cast<float>(imageSize.width),
            static_cast<float>(imageSize.height)};
  }

  return {0, 0};
}

}
