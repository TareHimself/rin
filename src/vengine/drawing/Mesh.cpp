#include "Mesh.hpp"

#include "Drawer.hpp"
#include "vengine/Engine.hpp"


namespace vengine {
namespace drawing {
Array<Vertex> Mesh::getVertices() const {
  return _asset.vertices;
}

Array<uint32_t> Mesh::getIndices() const {
  return _asset.indices;
}

Array<assets::MeshSurface> Mesh::getSurfaces() const {
  return _asset.surfaces;
}

Array<Material *> Mesh::getMaterials() const {
  return _materials;
}

void Mesh::setMaterial(uint32_t index, Material *material) {
  _materials[index] = material;
}

void Mesh::upload() {
  buffers = getOuter()->getRenderer()->createMeshBuffers(this);
}

String Mesh::getName() const {
  return _asset.name;
}

void Mesh::handleCleanup() {
  Object<Engine>::handleCleanup();
  
}

void Mesh::setAsset(const assets::MeshAsset &asset) {
  _asset = asset;
  _materials.clear();
  _materials.resize(_asset.surfaces.size());
}


}
}
