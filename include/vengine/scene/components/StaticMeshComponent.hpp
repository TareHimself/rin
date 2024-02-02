#pragma once
#include "RenderedComponent.hpp"
#include "vengine/drawing/Mesh.hpp"

namespace vengine::scene {
class StaticMeshComponent : public RenderedComponent {
  Pointer<drawing::Mesh> _mesh;
public:
  WeakPointer<drawing::Mesh> GetMesh() const;
  void SetMesh(const Pointer<drawing::Mesh> &newMesh);

  void Draw(drawing::SceneDrawer *drawer, drawing::SimpleFrameData *frameData) override;
  
  VENGINE_IMPLEMENT_COMPONENT_ID(StaticMeshComponent)
};


}
