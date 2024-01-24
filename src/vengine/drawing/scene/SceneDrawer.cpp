
#include "SceneDrawer.hpp"
#include "vengine/Engine.hpp"
#include "vengine/drawing/Drawer.hpp"
#include "vengine/drawing/MaterialBuilder.hpp"
#include "vengine/io/io.hpp"
#include "vengine/scene/Scene.hpp"
#include "vengine/scene/SceneObject.hpp"
#include "vengine/scene/components/CameraComponent.hpp"

#include <glm/gtx/transform.hpp>

namespace vengine::drawing {
Drawer *SceneDrawer::GetDrawer() {
  return GetOuter()->GetEngine()->GetDrawer();
}

void SceneDrawer::Init(scene::Scene * outer) {
  Object<scene::Scene>::Init(outer);
  _sceneGlobalBuffer = GetDrawer()->GetAllocator()->CreateUniformCpuGpuBuffer(sizeof(SceneGlobalBuffer),false);
  const auto drawExtent = GetDrawer()->GetEngine()->GetWindowExtent();
  _viewport.x = 0;
  _viewport.y = 0;
  _viewport.width = drawExtent.width;
  _viewport.height = drawExtent.height;
  _viewport.minDepth = 0.0f;
  _viewport.maxDepth = 1.0f;

  MaterialBuilder builder;
  _defaultCheckeredMaterial = builder
  .SetPass(EMaterialPass::Opaque)
  .ConfigurePushConstant<MeshVertexPushConstant>("pVertex")
  .AddShader(Shader::FromSource(GetDrawer()->GetShaderManager(), io::getRawShaderPath("3d/mesh/mesh.frag")))
  .AddShader(Shader::FromSource(GetDrawer()->GetShaderManager(), io::getRawShaderPath("3d/mesh/mesh.vert")))
  .Create(GetDrawer());

  
  const auto resources = _defaultCheckeredMaterial->GetResources();
  
  for(const auto &key : resources.images | std::views::keys) {
    _defaultCheckeredMaterial->SetTexture(key,GetDrawer()->GetDefaultBlackTexture());
  }

  _defaultCheckeredMaterial->SetTexture("ColorT",GetDrawer()->GetDefaultErrorCheckerboardTexture());

  _defaultCheckeredMaterial->SetBuffer<SceneGlobalBuffer>("SceneGlobalBuffer",_sceneGlobalBuffer.value());
  //= MaterialInstance::create(this,EMaterialPass::Opaque,_globalAllocator,materialResources);
  AddCleanup([=] {
    _defaultCheckeredMaterial->Destroy();
    _defaultCheckeredMaterial = nullptr;
  });
}

void SceneDrawer::Draw(Drawer *drawer, RawFrameData *frameData) {
  const auto cmd = frameData->GetCmd();
  
  const auto scene = GetOuter();

  

  cmd->setViewport(0, {_viewport});

  vk::Rect2D scissor{{0, 0}, {static_cast<uint32_t>(_viewport.width),static_cast<uint32_t>(_viewport.height)}};

  cmd->setScissor(0, {scissor});

  const auto cameraComponent = scene->GetViewTarget()->GetComponentByClass<scene::CameraComponent>();

  if(cameraComponent == nullptr) {
    GetDrawer()->GetLogger()->error("Failed to draw scene as no camera exists");
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
  const auto mappedData = _sceneGlobalBuffer.value().alloc.GetMappedData();
  const auto sceneUniformData = static_cast<SceneGlobalBuffer *>(mappedData);
  *sceneUniformData = _sceneData;

  //create a descriptor set that binds that buffer and update it

  drawing::SimpleFrameData drawData(frameData);
  
  for (const auto drawable : GetOuter()->GetSceneObjects()) {
    drawable->Draw(this, &drawData);
  }
}

void SceneDrawer::HandleDestroy() {
  Object<scene::Scene>::HandleDestroy();
  GetDrawer()->GetDevice().waitIdle();
  if(_sceneGlobalBuffer.has_value()) {
    GetDrawer()->GetAllocator()->DestroyBuffer(_sceneGlobalBuffer.value());
  }
}
}
