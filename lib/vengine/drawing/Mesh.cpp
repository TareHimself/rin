#include <vengine/drawing/Mesh.hpp>
#include <vengine/drawing/DrawingSubsystem.hpp>
#include "vengine/Engine.hpp"
#include "vengine/utils.hpp"


namespace vengine::drawing {
Ref<GpuGeometryBuffers> Mesh::GetGpuData() {
  return _gpuData;
}

Array<Vertex> Mesh::GetVertices() const {
  return _vertices;
}

Array<uint32_t> Mesh::GetIndices() const {
  return _indices;
}

Array<MeshSurface> Mesh::GetSurfaces() const {
  return _surfaces;
}

Array<Ref<MaterialInstance>> Mesh::GetMaterials() const {
  Array<Ref<MaterialInstance>> weakPtr;
  for(const auto &mat : _materials) {
    weakPtr.push(mat);
  }
  return weakPtr;
}

void Mesh::SetVertices(const Array<Vertex> &vertices) {
  _vertices = vertices;
}

void Mesh::SetIndices(const Array<uint32_t> &indices) {
  _indices = indices;
}

void Mesh::SetSurfaces(const Array<MeshSurface> &surfaces) {
  _surfaces = surfaces;
  _materials.resize(surfaces.size());
}

void Mesh::SetMaterial(uint32_t index,
                       const Managed<MaterialInstance> &material) {
  utils::vassert(index < _materials.size(),"Cannot set material index outside of range {}...{}",0,_materials.size() - 1);
  _materials[index] = material;
}

void Mesh::Upload() {
  if(!IsUploaded()) {
    _gpuData = GetOuter()->CreateGeometryBuffers(this);
  }
}

bool Mesh::IsUploaded() const {
  return _gpuData;
}

String Mesh::GetName() const {
  return GetHeader().name;
}

void Mesh::BeforeDestroy() {
  Object<DrawingSubsystem>::BeforeDestroy();
  if(_gpuData) {
    GetOuter()->WaitDeviceIdle();
    _gpuData.Clear();
  }
  
}

void Mesh::ReadFrom(Buffer &store) {
  store >> _vertices;
  store >> _indices;
  store >> _surfaces;
  _materials.resize(_surfaces.size());
}

void Mesh::WriteTo(Buffer &store) {
  store << _vertices;
  store << _indices;
  store << _surfaces;
}


}
