
#include <vengine/drawing/scene/SceneDrawer.hpp>
#include "vengine/Engine.hpp"
#include "vengine/drawing/DrawingSubsystem.hpp"
#include "vengine/drawing/MaterialBuilder.hpp"
#include "vengine/drawing/WindowDrawer.hpp"
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
  const auto drawExtent = GetOuter()->GetEngine()->GetMainWindowSize();
  

  auto shaderManager = drawer->GetShaderManager().Reserve();
  MaterialBuilder builder;
  _defaultCheckeredMaterial = CreateMaterialInstance({
    Shader::FromSource(io::getRawShaderPath("3d/mesh_deferred.frag")),
    Shader::FromSource(io::getRawShaderPath("3d/mesh.vert"))
  });
  
  
  const auto resources = _defaultCheckeredMaterial->GetResources();
  //
  for(const auto &key : resources.images | std::views::keys) {
    _defaultCheckeredMaterial->SetTexture(key,drawer->GetDefaultBlackTexture());
  }
  //
  _defaultCheckeredMaterial->SetTexture("ColorT",drawer->GetDefaultErrorCheckerboardTexture());
  //
  _defaultCheckeredMaterial->SetBuffer("SceneGlobalBuffer",_sceneGlobalBuffer);

  if (auto windowDrawer = drawer->GetWindowDrawer(Engine::Get()->GetMainWindow()).Reserve()) {
    _windowDrawer = windowDrawer;
    AddCleanup(windowDrawer->onDrawScenes, windowDrawer->onDrawScenes.Bind(
                   [this](drawing::RawFrameData *rawFrame) {
                     Draw(rawFrame);
                   }));
  }
}

void SceneDrawer::Draw(RawFrameData *frameData) {
  const auto cmd = frameData->GetCmd();
  
  const auto scene = GetOuter();
  
  const auto cameraRef = scene->GetViewTarget().Reserve()->GetComponentByClass<scene::CameraComponent>().Reserve();

  if(!cameraRef && cameraRef->IsInitialized()) {
    frameData->GetDrawer()->GetLogger()->warn("Skipping scene, no active camera");
    return;
  }

  const auto viewport = frameData->GetWindowDrawer()->GetViewport();
  _sceneData.viewMatrix = cameraRef->GetViewMatrix(); //glm::translate(glm::vec3{ 0,0,-5 }); glm::translate(glm::vec3{ 0,0,15 }); glm::translate(glm::vec3{ 0,0,15 });//
  // camera projection
  _sceneData.projectionMatrix = cameraRef->GetProjection(viewport.width / viewport.height);

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

  SceneFrameData drawData(frameData,this);
  
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

vk::Extent2D SceneDrawer::GetDrawExtent() const {
  return _drawExtent;
}

Ref<WindowDrawer> SceneDrawer::GetWindowDrawer() {
  return _windowDrawer;
}

Ref<MaterialInstance> SceneDrawer::GetDefaultMaterial() const {
  return _defaultCheckeredMaterial;
}

Managed<MaterialInstance> SceneDrawer::CreateMaterialInstance(const Array<Managed<Shader>> &shaders) {
  return MaterialBuilder().AddShaders(shaders).SetType(EMaterialType::Lit).AddAttachmentFormats(GetColorAttachmentFormats()).Create();
}

}
