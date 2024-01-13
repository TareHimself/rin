#include "StaticMeshComponent.hpp"

#include "vengine/Engine.hpp"
#include "vengine/drawing/Drawer.hpp"
#include "vengine/drawing/Material.hpp"
#include "vengine/drawing/scene/SceneDrawer.hpp"
#include "vengine/scene/SceneObject.hpp"
#include <glm/gtx/transform.hpp>

namespace vengine {
namespace scene {
drawing::Mesh * StaticMeshComponent::getMesh() const {
  return mesh;
}

void StaticMeshComponent::setMesh(drawing::Mesh *newMesh) {
  mesh = newMesh;
}

void StaticMeshComponent::draw(drawing::SceneDrawer *renderer,
    drawing::SceneFrameData *frameData) {
  if(mesh == nullptr) {
    return;
  }

  const auto transform = getWorldTransform().toMatrix();
    
  drawing::GpuDrawPushConstants pushConstants;

  pushConstants.worldMatrix = transform;//glm::mat4{1.f};
  pushConstants.vertexBuffer = mesh->buffers.vertexBufferAddress;

  const auto cmd = frameData->getCmd();
  
  const auto surfaces = mesh->getSurfaces();
  const auto materials = mesh->getMaterials();
  
  assert(surfaces.size() == materials.size(),"Surfaces and Materials Size Mismatch");
  
  for(auto i = 0; i < surfaces.size(); i++) {
    const auto surface = surfaces[i];
    const auto material = materials[i] == nullptr ? renderer->getEngineRenderer()->getDefaultCheckeredMaterial() : materials[i];

    material->bind(frameData);
    
    cmd->bindIndexBuffer(mesh->buffers.indexBuffer.buffer,0,vk::IndexType::eUint32);

    material->pushVertexConstant(frameData,sizeof(drawing::GpuDrawPushConstants),&pushConstants);
    
    cmd->drawIndexed(surface.count,1,surface.startIndex,0,0);
  }
}
}
}
