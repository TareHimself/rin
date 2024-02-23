#include "vengine/widget/Viewport.hpp"

#include "vengine/Engine.hpp"
#include "vengine/drawing/WindowDrawer.hpp"
#include "vengine/drawing/scene/SceneDrawer.hpp"
#include "vengine/io/io.hpp"
#include "vengine/widget/utils.hpp"

namespace vengine::widget {

void Viewport::Init(WidgetSubsystem *outer) {
  GeometryWidget::Init(outer);
  constexpr auto samplerInfo = vk::SamplerCreateInfo().
                               setMinFilter(vk::Filter::eLinear).setMagFilter(
                                   vk::Filter::eLinear);
  auto drawer = Engine::Get()->GetDrawingSubsystem().Reserve();
  _sampler = drawer->GetVirtualDevice().createSampler(samplerInfo);
  _shader = GetOuter()->CreateMaterialInstance(
      {drawing::Shader::FromSource(io::getRawShaderPath("2d/rect.vert")),
       drawing::Shader::FromSource(io::getRawShaderPath("2d/viewport.frag"))}
      );

  if (auto scene = Engine::Get()->GetScenes().at(0).Reserve()) {
    _shader->SetImage("TViewport",
                      scene->GetDrawer().Reserve()->GetRenderTarget(),
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

void Viewport::BeforeDestroy() {
  Canvas::BeforeDestroy();
  _shader.Clear();
  Engine::Get()->GetDrawingSubsystem().Reserve()->GetVirtualDevice().
                 destroySampler(_sampler);
}

void Viewport::OnAddedToScreen() {
  Canvas::OnAddedToScreen();
  _rootResizeHandle =  GetRoot()->GetWindowDrawer().Reserve()->onResizeUi.Bind([this] {
    if (auto scene = Engine::Get()->GetScenes().at(0).Reserve()) {
      _shader->SetImage("TViewport",
                        scene->GetDrawer().Reserve()->GetRenderTarget(),
                        _sampler);
    }
  });
}

void Viewport::OnRemovedFromScreen() {
  Canvas::OnRemovedFromScreen();
  if(_rootResizeHandle.has_value()) {
    GetRoot()->GetWindowDrawer().Reserve()->onResizeUi.UnBind(_rootResizeHandle.value());
  }
}
}
