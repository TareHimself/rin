#pragma once
#include "RenderedComponent.hpp"
#include "vengine/drawing/Mesh.hpp"

namespace vengine::scene {
class StaticMeshComponent : public RenderedComponent {
  Ref<drawing::Mesh> _mesh;
public:
  WeakRef<drawing::Mesh> GetMesh() const;
  void SetMesh(const Ref<drawing::Mesh> &newMesh);

  void Draw(drawing::SceneDrawer *drawer, const math::Transform &parentTransform, drawing::SimpleFrameData *frameData) override;
  
  VENGINE_IMPLEMENT_COMPONENT_ID(StaticMeshComponent)
};


}
