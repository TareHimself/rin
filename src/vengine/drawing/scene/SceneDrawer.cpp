
#include "SceneDrawer.hpp"
#include "vengine/Engine.hpp"
#include "vengine/drawing/Drawer.hpp"
#include "vengine/scene/Scene.hpp"
#include "vengine/scene/components/CameraComponent.hpp"

#include <glm/gtx/transform.hpp>

namespace vengine::drawing {
Drawer *SceneDrawer::GetEngineRenderer() {
  return GetOuter()->GetEngine()->GetDrawer();
}

void SceneDrawer::Init(scene::Scene * outer) {
  Object<scene::Scene>::Init(outer);
  _gpuSceneDataBuffer = GetEngineRenderer()->GetAllocator()->CreateUniformCpuGpuBuffer(sizeof(SceneGpuData),false);
}

void SceneDrawer::Draw(Drawer *drawer, FrameData *frameData) {
  auto drawData = SceneFrameData(frameData);
  const auto cmd = drawData.GetCmd();
  const auto drawExtent = drawer->GetEngine()->GetWindowExtent();
  const auto scene = GetOuter();

  _viewport.x = 0;
  _viewport.y = 0;
  _viewport.width = drawExtent.width;
  _viewport.height = drawExtent.height;
  _viewport.minDepth = 0.0f;
  _viewport.maxDepth = 1.0f;

  cmd->setViewport(0, {_viewport});

  vk::Rect2D scissor{{0, 0}, drawExtent};

  cmd->setScissor(0, {scissor});

  const auto cameraComponent = scene->GetViewTarget()->GetComponentByClass<scene::CameraComponent>();

  if(cameraComponent == nullptr) {
    log::drawing->error("Failed to draw scene as no camera exists");
    return;
  }
  
  _sceneData.viewMatrix = cameraComponent->GetViewMatrix(); //glm::translate(glm::vec3{ 0,0,-5 });
  // camera projection
  _sceneData.projectionMatrix = cameraComponent->GetProjection(_viewport.width / _viewport.height);
    
  // invert the Y direction on projection matrix so that we are more similar
  // to opengl and gltf axis
  // _sceneData.proj[1][1] *= -1;

  //some default lighting parameters
  _sceneData.ambientColor = glm::vec4(.1f);
  const auto loc = cameraComponent->GetWorldLocation();
  _sceneData.cameraLocation = glm::vec4{loc.x,loc.y,loc.z,0.0f};
  // const auto camForward = cameraComponent->GetWorldRotation().Forward();
  // _sceneData.lightDirection = glm::vec4{camForward.x,camForward.y,camForward.z,0.0f};
  // _sceneData.sunlightColor = glm::vec4(1.f);
  // _sceneData.sunlightDirection = glm::vec4(0,1,0.5,1.f);

  
  
  // drawData.GetCleaner()->Push([drawer,gpuSceneDataBuffer] {
  //   drawer->GetAllocator()->
  // });
  
  // Write the buffer
  const auto mappedData = _gpuSceneDataBuffer.value().alloc.GetMappedData();
  const auto sceneUniformData = static_cast<SceneGpuData *>(mappedData);
  *sceneUniformData = _sceneData;

  //create a descriptor set that binds that buffer and update it
  const vk::DescriptorSet sceneDescriptor = frameData->GetDescriptorAllocator()
      ->Allocate(drawer->GetSceneDescriptorLayout());
  
  DescriptorWriter writer;
  
  writer.WriteBuffer(0, _gpuSceneDataBuffer.value().buffer, sizeof(SceneGpuData), 0,                     vk::DescriptorType::eUniformBuffer);
  writer.UpdateSet(drawer->GetDevice(), sceneDescriptor);
  
  drawData.SetSceneDescriptor(sceneDescriptor);
  
  for (const auto drawable : GetOuter()->GetSceneObjects()) {
    drawable->Draw(this, &drawData);
  }
}

void SceneDrawer::HandleDestroy() {
  Object<scene::Scene>::HandleDestroy();
  GetEngineRenderer()->GetDevice().waitIdle();
  if(_gpuSceneDataBuffer.has_value()) {
    GetEngineRenderer()->GetAllocator()->DestroyBuffer(_gpuSceneDataBuffer.value());
  }
}
}
