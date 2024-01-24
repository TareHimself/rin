#include "WidgetManager.hpp"
#include "Widget.hpp"
#include "vengine/Engine.hpp"
#include "vengine/drawing/Drawer.hpp"
#include "vengine/drawing/MaterialBuilder.hpp"
#include "vengine/io/io.hpp"

namespace vengine::widget {
void WidgetManager::Init(Engine *outer) {
  EngineSubsystem::Init(outer);
  outer->onWindowSizeChanged.On([=](vk::Extent2D size) {
    log::engine->info("WIDGETS SYSTEM, WINDOW SIZE CHANGED");
    _windowSize = size;
  });
  const auto drawer = outer->GetDrawer();

  _uiGlobalBuffer = drawer->GetAllocator()->CreateUniformCpuGpuBuffer(sizeof(drawing::UiGlobalBuffer),false);

  drawing::MaterialBuilder builder;
  _defaultWidgetShader = builder
  .SetPass(drawing::EMaterialPass::UI)
  .AddShader(drawing::Shader::FromSource(drawer->GetShaderManager(), io::getRawShaderPath("2d/rect/rect.vert")))
  .AddShader(drawing::Shader::FromSource(drawer->GetShaderManager(), io::getRawShaderPath("2d/rect/rect.frag")))
  .ConfigurePushConstant<drawing::WidgetPushConstants>("pRect")
  .Create(drawer);

  // Set Global variable
  _defaultWidgetShader->SetBuffer<drawing::UiGlobalBuffer>("UiGlobalBuffer",_uiGlobalBuffer.value());
}

void WidgetManager::HandleDestroy() {
  EngineSubsystem::HandleDestroy();

  GetOuter()->GetDrawer()->GetDevice().waitIdle();
  
  if(_uiGlobalBuffer.has_value()) {
    GetOuter()->GetDrawer()->GetAllocator()->DestroyBuffer(_uiGlobalBuffer.value());
  }
  
  for(const auto widget : _topLevelWidgets) {
    widget->Destroy();
  }

  _topLevelWidgets.clear();

  _defaultWidgetShader->Destroy();
}

String WidgetManager::GetName() const {
  return "widgets";
}

void WidgetManager::Draw(drawing::Drawer *drawer,
    drawing::RawFrameData *frameData) {
  if(!_topLevelWidgets.empty()) {

    drawing::UiGlobalBuffer uiGb;
    uiGb.viewport = glm::vec4{0,0,_windowSize.width,_windowSize.height};
    
    const auto mappedData = _uiGlobalBuffer.value().alloc.GetMappedData();
    const auto uiGlobalBuffer = static_cast<drawing::UiGlobalBuffer *>(mappedData);
    *uiGlobalBuffer = uiGb;
    
    WidgetParentInfo myInfo;
    myInfo.extent = _windowSize;
    myInfo.widget = nullptr;

    drawing::SimpleFrameData wFrameData(frameData);
    
    for(const auto rootWidget : _topLevelWidgets) {
      rootWidget->Draw(drawer,&wFrameData,myInfo);
    }
  }
}

void WidgetManager::InitWidget(Widget *widget) {
  widget->Init(this);
  _topLevelWidgets.Push(widget);
}

drawing::MaterialInstance * WidgetManager::GetDefaultRectMaterial() const {
  return _defaultWidgetShader;
}
}
