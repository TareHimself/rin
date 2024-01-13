#include "AssetManager.hpp"

#include <vengine/io/io.hpp>
#include <fstream>
#include <fastgltf/glm_element_traits.hpp>
#include <fastgltf/parser.hpp>
#include <fastgltf/tools.hpp>

namespace vengine {
namespace assets {
bool AssetManager::loadAsset(const std::filesystem::path &path,
                              VEngineAsset &asset, bool loadData) {
  std::ifstream infile;
  infile.open(path, std::ios::binary);

  if (!infile.is_open()) return false;

  //move file cursor to beginning
  infile.seekg(0);

  infile >> asset.version;
  infile >> asset.type;
  infile >> asset.name;
  infile >> asset.meta;

  if(loadData) {
    asset.load(infile);
  }

  infile.close();
  
  return true;
}

bool AssetManager::saveAsset(const std::filesystem::path &path,
                              const VEngineAsset &asset) {
  std::ofstream outFile;
  outFile.open(path, std::ios::binary | std::ios::out);

  if (!outFile.is_open()) return false;
  
  outFile << asset.version;
  outFile << asset.type;
  outFile << asset.name;
  outFile << asset.meta;
  asset.save(outFile);
  
  outFile.close();

  return true;
}

std::optional<MeshAsset> AssetManager::importMesh(
    const std::filesystem::path &path) {
  if(!std::filesystem::exists(path)) {
    return std::nullopt;
  }

  fastgltf::GltfDataBuffer data;
  data.loadFromFile(path);

  constexpr auto gltfOptions = fastgltf::Options::LoadGLBBuffers | fastgltf::Options::LoadGLBBuffers;

  fastgltf::Asset gltf;
  fastgltf::Parser parser;

  if(auto load = parser.loadBinaryGLTF(&data,path.parent_path(),gltfOptions)) {
    gltf = std::move(load.get());
  } else {
    log::assets->error("Failed to load glTF: {} \n",fastgltf::to_underlying(load.error()));
    return std::nullopt;
  }

  // Only Import First Mesh
  if(gltf.meshes.empty()) {
    return std::nullopt;
  }

  auto meshToImport = gltf.meshes[0];

  MeshAsset meshAsset;
  meshAsset.name = String(meshToImport.name);
  auto indices = &meshAsset.indices;
  auto vertices = &meshAsset.vertices;
  for(auto && primitive : meshToImport.primitives) {
    MeshSurface newSurface;
    newSurface.startIndex = static_cast<uint32_t>(indices->size());
    newSurface.count = gltf.accessors[primitive.indicesAccessor.value()].count;

    auto initialVtx = vertices->size();
    // Load indexes
    {
      fastgltf::Accessor& indexAccessor = gltf.accessors[primitive.indicesAccessor.value()];
      indices->reserve(indices->size() + indexAccessor.count);
      fastgltf::iterateAccessor<uint32_t>(gltf,indexAccessor,[&](uint32_t idx) {
        auto index = static_cast<uint32_t>(idx + initialVtx);
        indices->push(index);
      });
    }

    // Load Vertex Positions
    {
      fastgltf::Accessor& posAccesor = gltf.accessors[primitive.findAttribute("POSITION")->second];
      vertices->resize(vertices->size() + posAccesor.count);

      fastgltf::iterateAccessorWithIndex<glm::vec3>(gltf,posAccesor,[&](glm::vec3 v, size_t idx) {
        drawing::Vertex newVertex;
        newVertex.location = v;
        newVertex.normal = {1,0,0};
        newVertex.color = glm::vec4{ 1.f};
        newVertex.uv_x = 0;
        newVertex.uv_y = 0;
        (*vertices)[initialVtx + idx] = newVertex;
      });
    }

    //Load Vertex Normal
    auto normals = primitive.findAttribute("NORMAL");
    if(normals != primitive.attributes.end()) {
      fastgltf::iterateAccessorWithIndex<glm::vec3>(gltf,gltf.accessors[(*normals).second],[&](glm::vec3 v,size_t idx) {
        (*vertices)[initialVtx + idx].normal = v;
      });
    }

    // Load UVs
    auto uv = primitive.findAttribute("TEXCOORD_0");
    if (uv != primitive.attributes.end()) {

      fastgltf::iterateAccessorWithIndex<glm::vec2>(gltf, gltf.accessors[(*uv).second],
          [&](glm::vec2 v, size_t idx) {
              (*vertices)[initialVtx + idx].uv_x = v.x;
              (*vertices)[initialVtx+ idx].uv_y = v.y;
          });
    }

    // display the vertex normals
    constexpr bool OverrideColors = true;
    if (OverrideColors) {
      for (drawing::Vertex& vtx : *vertices) {
        vtx.color = glm::vec4(vtx.normal, 1.f);
      }
    }
    meshAsset.surfaces.push(newSurface);
  }

  return meshAsset;
}

}
}
