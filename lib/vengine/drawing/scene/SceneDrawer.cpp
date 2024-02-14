
#include <vengine/drawing/scene/SceneDrawer.hpp>
#include "vengine/Engine.hpp"
#include "vengine/drawing/DrawingSubsystem.hpp"
#include "vengine/drawing/MaterialBuilder.hpp"
#include "vengine/io/io.hpp"
#include "vengine/scene/Scene.hpp"
#include "vengine/scene/objects/SceneObject.hpp"
#include "vengine/scene/components/CameraComponent.hpp"
#include "vengine/scene/components/LightComponent.hpp"

namespace vengine::drawing {
Ref<DrawingSubsystem> SceneDrawer::GetDrawer() {
  return GetOuter()->GetEngine()->GetDrawingSubsystem();
}

void SceneDrawer::Init(scene::Scene * outer) {
  Object<scene::Scene>::Init(outer);
  auto drawer = GetDrawer().Reserve();
  _sceneGlobalBuffer = drawer->GetAllocator().Reserve()->CreateUniformCpuGpuBuffer(sizeof(SceneGlobalBuffer),false);
  const auto drawExtent = GetOuter()->GetEngine()->GetWindowExtent();
  _viewport.x = 0;
  _viewport.y = 0;
  _viewport.width = drawExtent.width;
  _viewport.height = drawExtent.height;
  _viewport.minDepth = 0.0f;
  _viewport.maxDepth = 1.0f;

  auto shaderManager = drawer->GetShaderManager().Reserve();
  MaterialBuilder builder;
  _defaultCheckeredMaterial = builder
  .SetType(EMaterialType::Lit)
  .AddShader(Shader::FromSource(io::getRawShaderPath("3d/mesh.frag")))
  .AddShader(Shader::FromSource(io::getRawShaderPath("3d/mesh.vert")))
  .Create();
  
  
  const auto resources = _defaultCheckeredMaterial->GetResources();
  //
  for(const auto &key : resources.images | std::views::keys) {
    _defaultCheckeredMaterial->SetTexture(key,drawer->GetDefaultBlackTexture());
  }
  //
  _defaultCheckeredMaterial->SetTexture("ColorT",drawer->GetDefaultErrorCheckerboardTexture());
  //
  _defaultCheckeredMaterial->SetBuffer<SceneGlobalBuffer>("SceneGlobalBuffer",_sceneGlobalBuffer);

  AddCleanup(GetOuter()->GetEngine()->onWindowSizeChanged,GetOuter()->GetEngine()->onWindowSizeChanged.Bind([=](vk::Extent2D newWindowSize) {
    const auto sizeDiffX = newWindowSize.width / _viewport.width;
    const auto sizeDiffY = newWindowSize.height / _viewport.height;
    _viewport.width *= sizeDiffX;
    _viewport.height *= sizeDiffY;
  }));
}

void SceneDrawer::Draw(RawFrameData *frameData) {
  const auto cmd = frameData->GetCmd();
  
  const auto scene = GetOuter();

  cmd->setViewport(0, {_viewport});

  vk::Rect2D scissor{{0, 0}, {static_cast<uint32_t>(_viewport.width),static_cast<uint32_t>(_viewport.height)}};

  cmd->setScissor(0, {scissor});

  const auto cameraRef = scene->GetViewTarget().Reserve()->GetComponentByClass<scene::CameraComponent>().Reserve();

  if(!cameraRef && cameraRef->IsInitialized()) {
    frameData->GetDrawer()->GetLogger()->warn("Skipping scene, no active camera");
    return;
  }

  _sceneData.viewMatrix = cameraRef->GetViewMatrix(); //glm::translate(glm::vec3{ 0,0,-5 }); glm::translate(glm::vec3{ 0,0,15 }); glm::translate(glm::vec3{ 0,0,15 });//
  // camera projection
  _sceneData.projectionMatrix = cameraRef->GetProjection(_viewport.width / _viewport.height);

  //some default lighting parameters
  _sceneData.ambientColor = glm::vec4(.1f);
  const auto loc = cameraRef->GetWorldLocation();
  _sceneData.cameraLocation = glm::vec4{loc.x,loc.y,loc.z,0.0f};
  
  _sceneData.numLights.x = 0;
  for(const auto &light : GetOuter()->GetSceneLights()) {
    if(_sceneData.numLights.x == 1024) break;
    if(const auto lightRef = light.Reserve().Get()) {
      _sceneData.lights[static_cast<int>(_sceneData.numLights.x)] = lightRef->GetLightInfo();
      _sceneData.numLights.x++;
    }
  }


  // Write the buffer
  const auto mappedData = _sceneGlobalBuffer->GetMappedData();
  const auto sceneUniformData = static_cast<SceneGlobalBuffer *>(mappedData);
  *sceneUniformData = _sceneData;

  drawing::SimpleFrameData drawData(frameData);
  drawData.SetDrawer(this);
  for (const auto &drawable : GetOuter()->GetSceneObjects().clone()) {
    if(auto drawableRef = drawable.Reserve(); drawableRef->IsInitialized()) {
      drawableRef->Draw(&drawData, {});
    }
  }
}

void SceneDrawer::BeforeDestroy() {
  Object<scene::Scene>::BeforeDestroy();
  GetDrawer().Reserve()->WaitDeviceIdle();
  _defaultCheckeredMaterial.Clear();
  _sceneGlobalBuffer.Clear();
}

Ref<MaterialInstance> SceneDrawer::GetDefaultMaterial() const {
  return _defaultCheckeredMaterial;
}
}
