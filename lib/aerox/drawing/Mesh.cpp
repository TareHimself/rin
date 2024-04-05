#include <aerox/drawing/Mesh.hpp>
#include <aerox/drawing/DrawingSubsystem.hpp>
#include "aerox/Engine.hpp"
#include "aerox/utils.hpp"


namespace aerox::drawing {
std::weak_ptr<GpuGeometryBuffers> Mesh::GetGpuData() {
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

Array<std::weak_ptr<MaterialInstance>> Mesh::GetMaterials() const {
  Array<std::weak_ptr<MaterialInstance>> weakPtr;
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
                       const std::shared_ptr<MaterialInstance> &material) {
  utils::vassert(index < _materials.size(),"Cannot set material index outside of range {}...{}",0,_materials.size() - 1);
  _materials[index] = material;
}

void Mesh::Upload() {
  if(!IsUploaded()) {
    _gpuData = Engine::Get()->GetDrawingSubsystem().lock()->CreateGeometryBuffers(this);
  }
}

bool Mesh::IsUploaded() const {
  return static_cast<bool>(_gpuData);
}

String Mesh::GetName() const {
  return "";
}

void Mesh::OnDestroy() {
  Object::OnDestroy();
  if(_gpuData) {
    Engine::Get()->GetDrawingSubsystem().lock()->WaitDeviceIdle();
    _gpuData.reset();
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
