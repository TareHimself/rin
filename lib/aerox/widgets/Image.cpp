#include "aerox/drawing/Texture.hpp"
#include "aerox/widgets/utils.hpp"

#include <aerox/widgets/Image.hpp>
#include <aerox/widgets/WidgetSubsystem.hpp>
#include <aerox/Engine.hpp>
#include <aerox/drawing/MaterialBuilder.hpp>
#include <aerox/io/io.hpp>

namespace aerox::widgets {

void Image::OnInit(WidgetSubsystem * ref) {
  GeometryWidget::OnInit(ref);
  const auto drawer = Engine::Get()->GetDrawingSubsystem().lock();

  _imageMat = GetOwner()->CreateMaterialInstance(
  {drawing::Shader::FromSource(io::getRawShaderPath("2d/rect.vert")),
   drawing::Shader::FromSource(io::getRawShaderPath("2d/image.frag"))});
  _options = drawer->GetAllocator().lock()->CreateUniformCpuGpuBuffer<ImageGpuData>(false,"Image " + GetObjectInstanceId() + " options buffer");
  _imageMat->SetBuffer("options",_options);
  UpdateOptionsBuffer();
}

void Image::SetTexture(const std::shared_ptr<drawing::Texture> &image) {
  _image = image;
  if (_imageMat) {
    _imageMat->SetTexture("ImageT", _image);
  }
  UpdateOptionsBuffer();
  InvalidateCachedSize();
}


std::weak_ptr<drawing::Texture> Image::GetTexture() const {
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

void Image::OnDestroy() {
  Widget::OnDestroy();
  _imageMat.reset();
}

void Image::UpdateOptionsBuffer() {
  const ImageGpuData imageData{_tint,static_cast<bool>(_image)};
  _options->Write(imageData);
}

void Image::SetTint(const Color &tint) {
  _tint = tint;
  UpdateOptionsBuffer();
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
