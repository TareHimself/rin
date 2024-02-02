#include <vengine/widget/Image.hpp>
#include <vengine/widget/WidgetManager.hpp>
#include <vengine/Engine.hpp>
#include <vengine/drawing/MaterialBuilder.hpp>
#include <vengine/io/io.hpp>

namespace vengine::widget {
void Image::Init(WidgetManager * outer) {
  Widget::Init(outer);
  const auto drawer = outer->GetEngine()->GetDrawer().Reserve();
  drawing::MaterialBuilder builder;
  _imageMat = builder
  .SetType(drawing::EMaterialType::UI)
  .AddShader(drawing::Shader::FromSource(drawer->GetShaderManager().Reserve().Get(), io::getRawShaderPath("2d/rect.vert")))
  .AddShader(drawing::Shader::FromSource(drawer->GetShaderManager().Reserve().Get(), io::getRawShaderPath("2d/image.frag")))
  .ConfigurePushConstant<WidgetPushConstants>("pRect")
  .Create(drawer.Get());
  _imageMat->SetBuffer<UiGlobalBuffer>("UiGlobalBuffer",outer->GetGlobalBuffer());
}

void Image::SetTexture(const Pointer<drawing::Texture> &image) {
  _image = image;
}


WeakPointer<drawing::Texture> Image::GetTexture() const {
  return _image;
}

void Image::DrawSelf(drawing::Drawer * drawer,
                     drawing::SimpleFrameData *frameData, WidgetParentInfo parentInfo) {
  const auto material = _imageMat;
  const auto ogFrameData = frameData->GetRaw();
  WidgetPushConstants drawData{};

  const auto rect = GetRect();
  drawData.extent = glm::vec4{rect.offset.x,rect.offset.y,rect.extent.width,rect.extent.height};
  drawData.time.x = 0;
  const auto textureToDraw = _image ? _image : GetOuter()->GetEngine()->GetDrawer().Reserve()->GetDefaultErrorCheckerboardTexture();
  material->BindPipeline(ogFrameData);
  material->BindSets(ogFrameData);
  material->SetDynamicTexture(frameData->GetRaw(),"ImageT",textureToDraw);
  material->PushConstant(frameData->GetCmd(),"pRect",drawData);
  
  frameData->GetCmd()->draw(6,1,0,0);
}

void Image::HandleDestroy() {
  Widget::HandleDestroy();
  _imageMat.Clear();
}
}
