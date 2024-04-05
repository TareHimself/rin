#include "aerox/widgets/Viewport.hpp"

#include "aerox/Engine.hpp"
#include "aerox/drawing/WindowDrawer.hpp"
#include "aerox/drawing/scene/SceneDrawer.hpp"
#include "aerox/io/io.hpp"
#include "aerox/widgets/utils.hpp"

namespace aerox::widgets {

void Viewport::OnInit(WidgetSubsystem *ref) {
  Canvas::OnInit(ref);

  constexpr auto samplerInfo = vk::SamplerCreateInfo().
                               setMinFilter(vk::Filter::eLinear).setMagFilter(
                                   vk::Filter::eLinear);
  auto drawer = Engine::Get()->GetDrawingSubsystem().lock();
  _sampler = drawer->GetVirtualDevice().createSampler(samplerInfo);
  _shader = GetOwner()->CreateMaterialInstance(
      {drawing::Shader::FromSource(io::getRawShaderPath("2d/rect.vert")),
       drawing::Shader::FromSource(io::getRawShaderPath("2d/viewport.frag"))}
      );

  _scene = Engine::Get()->GetScenes().at(0);
  
  if (const auto scene = _scene.lock()) {
    _shader->SetImage("TViewport",
                      scene->GetDrawer().lock()->GetRenderTarget().lock(),
                      _sampler);
  }
}


void Viewport::OnPaint(WidgetFrameData *frameData, const Rect &clip) {
  const auto rect = GetDrawRect();

  if (!_shader) {
    return;
  }

  WidgetPushConstants drawData{};
  drawData.clip = clip;
  drawData.extent = rect;
  //drawData.transform = glm::rotate(glm::mat4{1.0f},45.0f,glm::vec3{0.0f,0.0,1.0f});

  bindMaterial(frameData, _shader);

  _shader->Push(frameData->GetCmd(), "pRect", drawData);

  frameData->DrawQuad();
}

void Viewport::OnDestroy() {
  Canvas::OnDestroy();
  _shader.reset();
  Engine::Get()->GetDrawingSubsystem().lock()->GetVirtualDevice().
                 destroySampler(_sampler);
}

void Viewport::OnAddedToScreen() {
  Canvas::OnAddedToScreen();
  _rootResizeHandle = GetRoot().lock()->GetWindowDrawer().lock()->onResizeUi->BindFunction([this] {
    if (const auto scene = Engine::Get()->GetScenes().at(0).lock()) {
      _shader->SetImage("TViewport",
                        scene->GetDrawer().lock()->GetRenderTarget().lock(),
                        _sampler);
    }
  });
}

void Viewport::OnRemovedFromScreen() {
  Canvas::OnRemovedFromScreen();
  if(_rootResizeHandle) {
    _rootResizeHandle->UnBind();
  }
}

}
