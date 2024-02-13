#include <vengine/scene/components/StaticMeshComponent.hpp>
#include "vengine/Engine.hpp"
#include "vengine/utils.hpp"
#include "vengine/drawing/DrawingSubsystem.hpp"
#include "vengine/drawing/MaterialInstance.hpp"
#include "vengine/drawing/scene/SceneDrawer.hpp"
#include "vengine/scene/objects/SceneObject.hpp"

namespace vengine::scene {
Ref<drawing::Mesh> StaticMeshComponent::GetMesh() const {
  return _mesh;
}

void StaticMeshComponent::SetMesh(const Managed<drawing::Mesh> &newMesh) {
  if(newMesh) {
    if(!newMesh->IsUploaded()) {
      newMesh->Upload();
    }
  }
  _mesh = newMesh;
}

void StaticMeshComponent::Draw(
    drawing::SimpleFrameData *frameData, const math::Transform &parentTransform) {
  if(!_mesh || !_mesh->IsUploaded()) {
    return;
  }
  const auto transform = GetWorldTransform();

  drawing::MeshVertexPushConstant pushConstants{};

  const auto meshGpuData = _mesh->GetGpuData().Reserve();
  pushConstants.transformMatrix = transform.Matrix(); //glm::mat4{1.f};
  pushConstants.vertexBuffer = meshGpuData->vertexBufferAddress;

  const auto rawFrameData = frameData->GetRaw();
  const auto cmd = frameData->GetCmd();
  
  const auto surfaces = _mesh->GetSurfaces();
  const auto materials = _mesh->GetMaterials();
  const auto surfaceMatSizeMatch = surfaces.size() == materials.size();
  utils::vassert(surfaceMatSizeMatch,"Surfaces and Materials Size Mismatch");
  
  for(auto i = 0; i < surfaces.size(); i++) {
    const auto [startIndex, count] = surfaces[i];
    const auto material = materials[i] ? materials[i].Reserve() : frameData->GetDrawer()->GetDefaultMaterial().Reserve();
    if(!material) {
      continue;
    }
    material->BindPipeline(rawFrameData);
    material->BindSets(rawFrameData);
    //material->BindCustomSet(rawFrameData,frameData->GetDescriptor(),0);
    material->PushConstant(frameData->GetCmd(),"pVertex",pushConstants);
    
    cmd->bindIndexBuffer(meshGpuData->indexBuffer->buffer,0,vk::IndexType::eUint32);
    cmd->drawIndexed(count,1,startIndex,0,0);
  }
}
}
