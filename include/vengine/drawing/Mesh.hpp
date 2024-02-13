#pragma once
#include "GpuNative.hpp"
#include "types.hpp"
#include "vengine/Object.hpp"
#include "vengine/assets/Asset.hpp"
#include "vengine/containers/Array.hpp"
#include "generated/drawing/Mesh.reflect.hpp"

namespace vengine::drawing {
class DrawingSubsystem;
}

namespace vengine::drawing {
class MaterialInstance;
}

namespace vengine::assets {
class AssetSubsystem;
}

namespace vengine {
class Engine;
}

namespace vengine::drawing {
struct MeshSurface {
  uint32_t startIndex;
  uint32_t count;
};

VENGINE_SIMPLE_ARRAY_SERIALIZER(Buffer, MeshSurface);

RCLASS()
class Mesh : public Object<DrawingSubsystem>, public assets::Asset, public GpuNative {

protected:
  Array<Vertex> _vertices;
  Array<uint32_t> _indices;
  Array<MeshSurface> _surfaces;
  Array<Managed<MaterialInstance>> _materials;
  Managed<GpuMeshBuffers> _gpuData;

public:
  Ref<GpuMeshBuffers> GetGpuData();
  Array<Vertex> GetVertices() const;
  Array<uint32_t> GetIndices() const;
  Array<MeshSurface> GetSurfaces() const;
  Array<Ref<MaterialInstance>> GetMaterials() const;


  void SetVertices(const Array<Vertex> &vertices);
  void SetIndices(const Array<uint32_t> &indices);
  void SetSurfaces(const Array<MeshSurface> &surfaces);
  void SetMaterial(uint32_t index, const Managed<MaterialInstance> &material);

  void Upload() override;
  bool IsUploaded() const override;
  String GetName() const;

  void BeforeDestroy() override;

  void ReadFrom(Buffer &store) override;

  void WriteTo(Buffer &store) override;

  RFUNCTION()
  static Managed<Mesh> Construct() { return newManagedObject<Mesh>(); }
  
  VENGINE_IMPLEMENT_REFLECTED_INTERFACE(Mesh)
};

REFLECT_IMPLEMENT(Mesh)
}
