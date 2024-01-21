#include "Mesh.hpp"
#include "Drawer.hpp"
#include "vengine/Engine.hpp"


namespace vengine::drawing {
Array<Vertex> Mesh::GetVertices() const {
  return _vertices;
}

Array<uint32_t> Mesh::GetIndices() const {
  return _indices;
}

Array<MeshSurface> Mesh::GetSurfaces() const {
  return _surfaces;
}

Array<MaterialInstance *> Mesh::GetMaterials() const {
  return _materials;
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

void Mesh::SetMaterial(const uint32_t index, MaterialInstance *material) {
  _materials[index] = material;
}

void Mesh::Upload() {
  if(!IsUploaded()) {
    gpuData = GetOuter()->CreateMeshBuffers(this);
  }
}

bool Mesh::IsUploaded() const {
  return gpuData.has_value();
}

String Mesh::GetName() const {
  return GetHeader().name;
}

void Mesh::HandleDestroy() {
  Object<Drawer>::HandleDestroy();
  if(IsUploaded()) {
    const auto [indexBuffer, vertexBuffer, vertexBufferAddress] = gpuData.value();
    GetOuter()->GetDevice().waitIdle();
    GetOuter()->GetAllocator()->DestroyBuffer(indexBuffer);
    GetOuter()->GetAllocator()->DestroyBuffer(vertexBuffer);
    gpuData.reset();
  }
}

String Mesh::GetSerializeId() {
  return "MESH";
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
