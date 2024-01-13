#include "SceneDrawer.hpp"

#include "SceneDrawable.hpp"
#include <vk_mem_alloc.hpp>
#include "vengine/Engine.hpp"
#include "vengine/drawing/Drawer.hpp"
#include "vengine/scene/Scene.hpp"
#include <glm/gtx/transform.hpp>

namespace vengine {
namespace drawing {
Drawer *SceneDrawer::getEngineRenderer() {
  return getOuter()->getEngine()->getRenderer();
}

void SceneDrawer::init(scene::Scene *outer) {
  Object<scene::Scene>::init(outer);
}

void SceneDrawer::draw(Drawer *drawer, FrameData *frameData) {
  auto drawData = SceneFrameData(frameData);
  const auto cmd = drawData.getCmd();
  const auto drawExtent = drawer->getDrawImageExtent();

  _viewport.x = 0;
  _viewport.y = 0;
  _viewport.width = drawExtent.width;
  _viewport.height = drawExtent.height;
  _viewport.minDepth = 0.0f;
  _viewport.maxDepth = 1.0f;

  cmd->setViewport(0, {_viewport});

  vk::Rect2D scissor{{0, 0}, drawExtent};

  cmd->setScissor(0, {scissor});
  
  _sceneData.view = glm::translate(glm::vec3{ 0,0,-5 });
  // camera projection
  _sceneData.proj = glm::perspective(glm::radians(70.f), _viewport.width / _viewport.height,  0.1f,10000.f);
    
  // invert the Y direction on projection matrix so that we are more similar
  // to opengl and gltf axis
  _sceneData.proj[1][1] *= -1;
  _sceneData.viewproj = _sceneData.proj * _sceneData.view;

  //some default lighting parameters
  _sceneData.ambientColor = glm::vec4(.1f);
  _sceneData.sunlightColor = glm::vec4(1.f);
  _sceneData.sunlightDirection = glm::vec4(0,1,0.5,1.f);

  // Should allocate this once later (doing this way for vkguide)
  AllocatedBuffer gpuSceneDataBuffer = drawer->createUniformCpuGpuBuffer(sizeof(GPUSceneData),true);
  
  drawData.getCleaner()->push([drawer,gpuSceneDataBuffer] {
    drawer->destroyBuffer(gpuSceneDataBuffer);
  });
  
  // Write the buffer
  const auto mappedData = gpuSceneDataBuffer.info.pMappedData;
  const auto sceneUniformData = static_cast<GPUSceneData *>(mappedData);
  *sceneUniformData = _sceneData;

  //create a descriptor set that binds that buffer and update it
  const vk::DescriptorSet sceneDescriptor = frameData->getDescriptorAllocator()
      ->allocate(drawer->getSceneDescriptorLayout());
  
  DescriptorWriter writer;
  
  writer.writeBuffer(0, gpuSceneDataBuffer.buffer, sizeof(GPUSceneData), 0,
                     vk::DescriptorType::eUniformBuffer);
  writer.updateSet(drawer->getDevice(), sceneDescriptor);
  
  drawData.setSceneDescriptor(sceneDescriptor);
  
  for (const auto drawable : getOuter()->getSceneObjects()) {
    drawable->draw(this, &drawData);
  }
}
}
}
