#include <vengine/widget/WidgetManager.hpp>
#include <vengine/widget/Widget.hpp>
#include <vengine/Engine.hpp>
#include <vengine/utils.hpp>
#include <vengine/assets/AssetManager.hpp>
#include <vengine/drawing/Drawer.hpp>
#include <vengine/drawing/MaterialBuilder.hpp>
#include <vengine/io/io.hpp>

namespace vengine::widget {
WeakPointer<drawing::AllocatedBuffer> WidgetManager::GetGlobalBuffer() const {
  return _uiGlobalBuffer;
}

void WidgetManager::Init(Engine * outer) {
  EngineSubsystem::Init(outer);

  const auto engine = GetEngine();
  
  engine->onWindowSizeChanged.On([=](vk::Extent2D size) {
    log::engine->info("WIDGETS SYSTEM, WINDOW SIZE CHANGED");
    _windowSize = size;
  });

  
  _windowSize = engine->GetWindowExtent();
  const auto drawer = engine->GetDrawer().Reserve();

  _uiGlobalBuffer = drawer->GetAllocator().Reserve()->CreateUniformCpuGpuBuffer(sizeof(UiGlobalBuffer),false);

  {
    drawing::MaterialBuilder builder;
    _defaultWidgetMat = builder
    .SetType(drawing::EMaterialType::UI)
    .AddShader(drawing::Shader::FromSource(drawer->GetShaderManager().Reserve().Get(), io::getRawShaderPath("2d/rect.vert")))
    .AddShader(drawing::Shader::FromSource(drawer->GetShaderManager().Reserve().Get(), io::getRawShaderPath("2d/rect.frag")))
    .ConfigurePushConstant<WidgetPushConstants>("pRect")
    .Create(drawer.Get());

    // Set Global variable
    _defaultWidgetMat->SetBuffer<UiGlobalBuffer>("UiGlobalBuffer",_uiGlobalBuffer);
  }
}

void WidgetManager::HandleDestroy() {
  EngineSubsystem::HandleDestroy();

  GetOuter()->GetDrawer().Reserve()->GetDevice().waitIdle();
  
  _uiGlobalBuffer.Clear();

  _topLevelWidgets.clear();

  _defaultWidgetMat.Clear();
}

String WidgetManager::GetName() const {
  return "widgets";
}

void WidgetManager::Draw(drawing::Drawer *drawer,
                         drawing::RawFrameData *frameData) {
  if(!_topLevelWidgets.empty()) {

    UiGlobalBuffer uiGb;
    uiGb.viewport = glm::vec4{0,0,_windowSize.width,_windowSize.height};
    
    const auto mappedData = _uiGlobalBuffer->GetMappedData();
    const auto uiGlobalBuffer = static_cast<UiGlobalBuffer *>(mappedData);
    *uiGlobalBuffer = uiGb;
    
    WidgetParentInfo myInfo;
    myInfo.rect = vk::Rect2D{{0,0},_windowSize};
    myInfo.widget = nullptr;

    drawing::SimpleFrameData wFrameData(frameData);
    for(const auto &widget : _topLevelWidgets.Clone()) {
      widget->Draw(drawer,&wFrameData,myInfo);
    }
  }
}

void WidgetManager::InitWidget(const Pointer<Widget> &widget) {
  widget->Init(this);
  _topLevelWidgets.Push(widget);
}

WeakPointer<drawing::MaterialInstance> WidgetManager::GetDefaultMaterial() const {
  return _defaultWidgetMat;
}
}
