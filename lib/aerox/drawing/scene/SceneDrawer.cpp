
#include <aerox/drawing/scene/SceneDrawer.hpp>
#include "aerox/Engine.hpp"
#include "aerox/drawing/DrawingSubsystem.hpp"
#include "aerox/drawing/MaterialBuilder.hpp"
#include "aerox/drawing/WindowDrawer.hpp"
#include "aerox/scene/Scene.hpp"

namespace aerox::drawing {

std::weak_ptr<DrawingSubsystem> SceneDrawer::GetDrawer() {
  return GetOwner()->GetEngine()->GetDrawingSubsystem();
}

void SceneDrawer::OnInit(scene::Scene * owner) {
  TOwnedBy::OnInit(owner);
  auto drawer = GetDrawer().lock();

  if (auto windowDrawer = drawer->GetWindowDrawer(Engine::Get()->GetMainWindow()).lock()) {
    _windowDrawer = windowDrawer;
    AddCleanup(windowDrawer->onDrawScenes->BindFunction(
                   [this](drawing::RawFrameData *rawFrame) {
                     Draw(rawFrame);
                   }));
  }
}

// void SceneDrawer::Draw(RawFrameData *frameData) {
//   const auto cmd = frameData->GetCmd();
//   
//   const auto scene = GetScene().Reserve();
//   
//   const auto cameraRef = scene->GetViewTarget().Reserve()->GetComponentByClass<scene::CameraComponent>().Reserve();
//
//   if(!cameraRef.IsValid() || !cameraRef->IsInitialized()) {
//     frameData->GetDrawer()->GetLogger()->Warn("Skipping scene, no active camera");
//     return;
//   }
//
//   const auto viewport = frameData->GetWindowDrawer()->GetViewport();
//   _sceneData.viewMatrix = cameraRef->GetViewMatrix(); //glm::translate(glm::vec3{ 0,0,-5 }); glm::translate(glm::vec3{ 0,0,15 }); glm::translate(glm::vec3{ 0,0,15 });//
//   // camera projection
//   _sceneData.projectionMatrix = cameraRef->GetProjection(viewport.width / viewport.height);
//
//   //some default lighting parameters
//   _sceneData.ambientColor = glm::vec4(.1f);
//   const auto loc = cameraRef->GetWorldLocation();
//   _sceneData.cameraLocation = glm::vec4{loc.x,loc.y,loc.z,0.0f};
//   
//   _sceneData.numLights.x = 0;
//   for(const auto &light : scene->GetSceneLights()) {
//     if(_sceneData.numLights.x == 1024) break;
//     if(const auto lightRef = light.Reserve().Get()) {
//       _sceneData.lights[static_cast<int>(_sceneData.numLights.x)] = lightRef->GetLightInfo();
//       _sceneData.numLights.x++;
//     }
//   }
//
//
//   // Write the buffer
//   _sceneGlobalBuffer->Write(_sceneData);
//
//   SceneFrameData drawData(frameData,this);
//   
//   for (const auto &drawable : scene->GetSceneObjects().clone()) {
//     if(auto drawableRef = drawable.Reserve(); drawableRef->IsInitialized()) {
//       drawableRef->Draw(&drawData, {});
//     }
//   }
//
//   
// }

vk::Extent2D SceneDrawer::GetDrawExtent() const {
  return _drawExtent;
}

std::weak_ptr<WindowDrawer> SceneDrawer::GetWindowDrawer() {
  return _windowDrawer;
}

}
