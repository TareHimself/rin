#define STB_IMAGE_IMPLEMENTATION
#include "AssetManager.hpp"
#include "vengine/Engine.hpp"
#include "vengine/drawing/Mesh.hpp"
#include "vengine/drawing/Texture.hpp"
#include <vengine/io/io.hpp>
#include <fstream>
#include <fastgltf/glm_element_traits.hpp>
#include <fastgltf/parser.hpp>
#include <fastgltf/tools.hpp>
#include <opencv2/imgcodecs.hpp>
#include <opencv2/imgproc.hpp>
namespace vengine::assets {

bool AssetManager::SaveAsset(const std::filesystem::path &path,
                             Asset *asset) {
  OutFileBuffer outFile(path);
  if (!outFile.isOpen())
    return false;

  auto header = asset->GetHeader();
  outFile << header;

  MemoryBuffer assetData;
  asset->WriteTo(assetData);
  const uint64_t dataSize = assetData.size();
  outFile << dataSize;
  outFile << assetData;

  outFile << assetData;

  outFile.close();

  return true;
}

drawing::Mesh *AssetManager::ImportMesh(
    const std::filesystem::path &path) {
  if (!std::filesystem::exists(path)) {
    return nullptr;
  }

  fastgltf::GltfDataBuffer data;
  data.loadFromFile(path);

  constexpr auto gltfOptions = fastgltf::Options::LoadGLBBuffers |
                               fastgltf::Options::LoadGLBBuffers;

  fastgltf::Asset gltf;
  fastgltf::Parser parser;
  if (auto load = parser.
      loadGltfBinary(&data, path.parent_path(), gltfOptions)) {
    gltf = std::move(load.get());
  } else {
    GetLogger()->error("Failed to load glTF: {} \n",
                       fastgltf::to_underlying(load.error()));
    return nullptr;
  }

  // Only Import First Mesh
  if (gltf.meshes.empty()) {
    return nullptr;
  }

  auto [primitives, weights, name] = gltf.meshes[0];

  Array<drawing::MeshSurface> surfaces;
  Array<uint32_t> indices;
  Array<drawing::Vertex> vertices;
  VEngineAssetHeader header;
  header.name = String(name);
  header.type = types::MESH;

  for (auto &&primitive : primitives) {
    drawing::MeshSurface newSurface{};
    newSurface.startIndex = static_cast<uint32_t>(indices.size());
    newSurface.count = gltf.accessors[primitive.indicesAccessor.value()].count;

    auto initialVtx = vertices.size();
    // Load indexes
    {
      fastgltf::Accessor &indexAccessor = gltf.accessors[primitive.
        indicesAccessor.value()];
      indices.reserve(indices.size() + indexAccessor.count);
      fastgltf::iterateAccessor<uint32_t>(gltf, indexAccessor,
                                          [&](const uint32_t idx) {
                                            auto index = static_cast<uint32_t>(
                                              idx + initialVtx);
                                            indices.Push(index);
                                          });
    }

    // Load Vertex Locations
    {
      fastgltf::Accessor &locationAccessor = gltf.accessors[primitive.
        findAttribute("POSITION")->second];
      vertices.resize(vertices.size() + locationAccessor.count);

      fastgltf::iterateAccessorWithIndex<glm::vec3>(
          gltf, locationAccessor, [&](glm::vec3 v, const size_t idx) {
            const drawing::Vertex newVertex{{v.x, v.y, v.z, 0}, {1, 0, 0, 0},
                                            {0, 0, 0, 0}};
            vertices[initialVtx + idx] = newVertex;
          });
    }

    // Load Vertex Normals
    if (auto normals = primitive.findAttribute("NORMAL");
      normals != primitive.attributes.end()) {
      fastgltf::iterateAccessorWithIndex<glm::vec3>(
          gltf, gltf.accessors[(*normals).second],
          [&](glm::vec3 v, const size_t idx) {
            vertices[initialVtx + idx].normal = {v.x, v.y, v.z, 0};
          });
    }

    // Load Vertex UVs
    if (auto uv = primitive.findAttribute("TEXCOORD_0");
      uv != primitive.attributes.end()) {

      fastgltf::iterateAccessorWithIndex<glm::vec2>(
          gltf, gltf.accessors[(*uv).second],
          [&](glm::vec2 v, const size_t idx) {
            vertices[initialVtx + idx].uv = {v.x, v.y, 0, 0};
          });
    }
    surfaces.Push(newSurface);
  }

  auto result = newObject<drawing::Mesh>();
  result->SetVertices(vertices);
  result->SetIndices(indices);
  result->SetSurfaces(surfaces);
  result->SetHeader(header);
  result->Init(GetOuter()->GetDrawer());

  GetLogger()->info("Imported Mesh {}",path.string());
  return result;
}

drawing::Mesh *AssetManager::LoadMeshAsset(
    const std::filesystem::path &path) {
  InFileBuffer inFile(path);
  if (!inFile.isOpen())
    return nullptr;

  VEngineAssetHeader header;
  inFile >> header;

  if (header.type != types::MESH)
    return nullptr;

  uint64_t dataSize;

  inFile >> dataSize;

  MemoryBuffer assetData;

  inFile >> assetData;

  inFile.close();

  const auto mesh = newObject<drawing::Mesh>();

  mesh->ReadFrom(assetData);
  mesh->Init(GetOuter()->GetDrawer());
  return mesh;
}

drawing::Texture * AssetManager::ImportTexture(
    const std::filesystem::path &path) {
  int width,height, nChannels;

  auto img = cv::imread(path.string(),cv::IMREAD_UNCHANGED);
  if(img.channels() == 3) {
    cv::cvtColor(img.clone(),img,cv::COLOR_BGR2RGBA);
  } else {
    cv::cvtColor(img.clone(),img,cv::COLOR_BGRA2RGBA);
  }
  
  const vk::Extent3D imageSize{static_cast<uint32_t>(img.cols) ,static_cast<uint32_t>(img.rows),1};
  Array<unsigned char> vecData;
  vecData.insert(vecData.end(),img.data,img.data + (imageSize.width * imageSize.height * img.channels()));
  const auto tex = drawing::Texture::FromData(GetOuter()->GetDrawer(),vecData,imageSize,vk::Format::eR8G8B8A8Unorm,vk::Filter::eLinear);
  VEngineAssetHeader header{};
  header.type = types::TEXTURE;
  header.name = path.filename().string();
  tex->SetHeader(header);
  GetLogger()->info("Imported Texture {}",path.string());
  return tex;
}

drawing::Texture * AssetManager::LoadTextureAsset(
    const std::filesystem::path &path) {
  InFileBuffer inFile(path);
  if (!inFile.isOpen())
    return nullptr;

  VEngineAssetHeader header;
  inFile >> header;

  if (header.type != types::MESH)
    return nullptr;

  uint64_t dataSize;

  inFile >> dataSize;

  MemoryBuffer assetData;

  inFile >> assetData;

  inFile.close();

  const auto texture = newObject<drawing::Texture>();

  texture->ReadFrom(assetData);
  texture->Init(GetOuter()->GetDrawer());
  return texture;
}

String AssetManager::GetName() const {
  return "assets";
}

}
