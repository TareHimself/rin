#include <aerox/scene/components/StaticMeshComponent.hpp>
#include "aerox/Engine.hpp"
#include "aerox/utils.hpp"
#include "aerox/drawing/DrawingSubsystem.hpp"
#include "aerox/drawing/MaterialInstance.hpp"
#include "aerox/drawing/scene/SceneDrawer.hpp"
#include "aerox/scene/objects/SceneObject.hpp"

namespace aerox::scene {
std::weak_ptr<drawing::Mesh> StaticMeshComponent::GetMesh() const {
  return _mesh;
}

void StaticMeshComponent::SetMesh(const std::shared_ptr<drawing::Mesh> &newMesh) {
  if (newMesh) {
    if (!newMesh->IsUploaded()) {
      newMesh->Upload();
    }
  }
  _mesh = newMesh;
}

void StaticMeshComponent::Draw(
    drawing::SceneFrameData *frameData,
    const math::Transform &parentTransform) {
  if (!_mesh || !_mesh->IsUploaded()) {
    return;
  }
  const auto transform = GetWorldTransform();

  drawing::MeshVertexPushConstant pushConstants{};

  const auto meshGpuData = _mesh->GetGpuData().lock();
  pushConstants.transformMatrix = transform.Matrix(); //glm::mat4{1.f};
  pushConstants.vertexBuffer = meshGpuData->vertexBufferAddress;

  const auto surfaces = _mesh->GetSurfaces();
  const auto materials = _mesh->GetMaterials();
  const auto surfaceMatSizeMatch = surfaces.size() == materials.size();
  utils::vassert(surfaceMatSizeMatch, "Surfaces and Materials Size Mismatch");

  for (auto i = 0; i < surfaces.size(); i++) {
    const auto [startIndex, count] = surfaces[i];
    const auto material = !materials[i].expired()
                            ? materials[i].lock()
                            : frameData->GetSceneDrawer()->GetDefaultMaterial().lock();
    if (!material) {
      continue;
    }

    frameData->AddLit(
        [pushConstants,material,meshGpuData,count,startIndex](
        const drawing::SceneFrameData *frame) {
          material->BindPipeline(frame->GetRaw());
          material->BindSets(frame->GetRaw());
          material->Push(frame->GetCmd(), "pVertex", pushConstants);

          frame->GetCmd()->bindIndexBuffer(meshGpuData->indexBuffer->buffer, 0,
                                           vk::IndexType::eUint32);
          frame->GetCmd()->drawIndexed(count, 1, startIndex, 0, 0);
        });
  }
}
}
