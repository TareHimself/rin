#pragma once
#include "RenderedComponent.hpp"
#include "vengine/IReflected.hpp"
#include "vengine/drawing/Mesh.hpp"
#include "generated/scene/components/StaticMeshComponent.reflect.hpp"
namespace vengine::scene {
RCLASS()
class StaticMeshComponent : public RenderedComponent {
  Managed<drawing::Mesh> _mesh;

public:
  Ref<drawing::Mesh> GetMesh() const;
  void SetMesh(const Managed<drawing::Mesh> &newMesh);

  void Draw(
      drawing::SimpleFrameData *frameData,
      const math::Transform &parentTransform) override;

  RFUNCTION()

  static Managed<StaticMeshComponent> Construct() {
    return newManagedObject<StaticMeshComponent>();
  }

  VENGINE_IMPLEMENT_REFLECTED_INTERFACE(StaticMeshComponent)
};

REFLECT_IMPLEMENT(StaticMeshComponent)
}
