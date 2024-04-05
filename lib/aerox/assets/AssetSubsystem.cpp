#include <aerox/assets/AssetSubsystem.hpp>
#include "aerox/Engine.hpp"
#include "aerox/drawing/Font.hpp"
#include "aerox/drawing/Mesh.hpp"
#include "aerox/drawing/Texture.hpp"
#include <aerox/io/io.hpp>
#include <fstream>
#include <fastgltf/glm_element_traits.hpp>
#include <fastgltf/parser.hpp>
#include <fastgltf/tools.hpp>
#include <simdjson.h>
#define STB_IMAGE_IMPLEMENTATION

#include "aerox/utils.hpp"
#include "aerox/assets/Image.hpp"
#include "aerox/assets/LiveAsset.hpp"
#include <stb_image.h>
#include <uuid.h>
#include <msdfgen.h>
#include <glm/glm.hpp>


#define F26DOT6_TO_DOUBLE(x) (1/64.*double(x))
#define F16DOT16_TO_DOUBLE(x) (1/65536.*double(x))
#define DOUBLE_TO_F16DOT16(x) FT_Fixed(65536.*x)


namespace aerox::assets {

void AssetSubsystem::OnInit(Engine *engine) {
  EngineSubsystem::OnInit(engine);
  const auto library = new FT_Library;

  if (FT_Init_FreeType(library)) {
    delete library;
    utils::verror("Failed to load freetype");
  }

  _library = std::shared_ptr<FT_Library>(library, [](FT_Library *ptr) {
    FT_Done_FreeType(*ptr);
    delete ptr;
  });
}

std::string AssetSubsystem::CreateAssetId() {
  return to_string(uuids::uuid_system_generator{}());
}

std::shared_ptr<AssetMeta> AssetSubsystem::CreateAssetMeta(
    const std::string &type, const std::set<std::string> &tags) {
  auto meta = std::make_shared<AssetMeta>();
  meta->id = CreateAssetId();
  meta->tags = tags;
  meta->type = type;
  meta->version = 0;
  return meta;
}

std::shared_ptr<AssetMeta> AssetSubsystem::LoadAssetMeta(const fs::path &path) {

  InFileBuffer inFile(path);
  if (!inFile.isOpen())
    return {};

  auto meta = CreateAssetMeta("", {});
  const auto oldId = meta->id;
  meta->ReadFrom(inFile);

  utils::vassert(oldId != meta->id, "How have you done this");
  _assets[meta->id] = std::make_shared<AssetInfo>(meta, path);

  inFile.close();

  return meta;
}

bool AssetSubsystem::SaveAssetMeta(const std::string &assetId) {
  if (!_assets.contains(assetId) || !_assets[assetId]) {
    return false;
  }

  auto assetInfo = _assets[assetId];

  OutFileBuffer outFile(assetInfo->path.string() + ".vm");
  if (!outFile.isOpen())
    return false;

  outFile << *assetInfo->meta.get();

  MemoryBuffer assetData;

  outFile.close();

  return true;
}

std::weak_ptr<AssetMeta> AssetSubsystem::FindAssetMeta(const std::string &assetId) {
  return _assets.contains(assetId) ? _assets[assetId]->meta : std::weak_ptr<AssetMeta>();
}

std::weak_ptr<AssetMeta> AssetSubsystem::CreateAsset(const fs::path &destPath,
                                           const std::function<std::shared_ptr<
                                             AssetMeta>()> &method) {
  if (auto meta = method()) {
    _assets[meta->id] = std::make_shared<AssetInfo>(meta, destPath);
    SaveAssetMeta(meta->id);
  }

  return {};
}


bool AssetSubsystem::SaveAsset(const std::shared_ptr<LiveAsset> &asset) {
  if (!_assets.contains(asset->GetAssetId())) {
    return false;
  }

  auto assetInfo = _assets[asset->GetAssetId()];

  OutFileBuffer outFile(assetInfo->path.string() + ".vd");

  if (!outFile.isOpen())
    return false;

  MemoryBuffer assetData;
  asset->WriteTo(assetData);

  const uint64_t dataSize = assetData.size();
  outFile << dataSize;
  outFile << assetData;

  outFile.close();

  return true;
}

std::shared_ptr<LiveAsset> AssetSubsystem::ImportAsset(const fs::path &path,
                                               const std::set<std::string> &
                                               tags,
                                               const std::function<std::shared_ptr<
                                                 LiveAsset>(
                                                   const fs::path &)> &
                                               importFn) {
  if (auto imported = importFn(path)) {
    auto meta = CreateAssetMeta(imported->GetMeta()->GetName(), tags);
    imported->SetAssetId(meta->id);
    return imported;
  }

  return {};
}

std::shared_ptr<LiveAsset> AssetSubsystem::LoadAsset(const std::string &assetId) {
  if (const auto assetInfo = _assets.find(assetId);
    assetInfo != _assets.end()) {
    if (const auto reflectedType = meta::find(
        assetInfo->second->meta->type); reflectedType &&
                                        reflectedType->HasFunction(
                                            "Construct")) {

      std::shared_ptr<LiveAsset> result;
      reflectedType->FindFunction("Construct")->CallStatic(&result);

      const auto dataFile = assetInfo->second->path.string() + ".vd";
      if (!fs::exists(dataFile)) {
        return result;
      }
      InFileBuffer inFile(dataFile);

      uint64_t dataSize;

      inFile >> dataSize;

      MemoryBuffer assetData;

      inFile >> assetData;

      inFile.close();

      result->ReadFrom(assetData);

      return result;

    }
  }

  return {};
}


std::shared_ptr<drawing::Mesh> AssetSubsystem::ImportMesh(const fs::path &path) {

  if (!fs::exists(path)) {
    return {};
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
    GetLogger()->Error("Failed to load glTF: {} \n",
                       fastgltf::to_underlying(load.error()));
    return {};
  }

  // Only Import First Mesh
  if (gltf.meshes.empty()) {
    return {};
  }

  auto [primitives, weights, name] = gltf.meshes[0];

  Array<drawing::MeshSurface> surfaces;
  Array<uint32_t> indices;
  Array<drawing::Vertex> vertices;

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
                                            indices.push(index);
                                          });
    }

    // Load Vertex Locations
    {
      fastgltf::Accessor &locationAccessor = gltf.accessors[primitive.
        findAttribute("POSITION")->second];
      vertices.resize(vertices.size() + locationAccessor.count);

      fastgltf::iterateAccessorWithIndex<glm::vec3>(
          gltf, locationAccessor, [&](glm::vec3 v, const size_t idx) {
            const drawing::Vertex newVertex{{v.x, v.y, v.z, 0},
                                            {1, 0, 0, 0},
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
    surfaces.push(newSurface);
  }

  auto result = newObject<drawing::Mesh>();

  result->SetVertices(vertices);
  result->SetIndices(indices);
  result->SetSurfaces(surfaces);

  return result;
}

std::shared_ptr<drawing::Mesh> AssetSubsystem::ImportMeshAsset(const fs::path &path) {
  return utils::cast<drawing::Mesh>(ImportAsset(path, {"mesh"}, [this](const fs::path &path) {
    return ImportMesh(path);
  }));
}

std::shared_ptr<drawing::Texture> AssetSubsystem::ImportTextureAsset(
    const fs::path &path) {
  return utils::cast<drawing::Texture>(ImportAsset(path, {"texture"}, [this](const fs::path &path) {
    return ImportTexture(path);
  }));
}

std::shared_ptr<drawing::Font> AssetSubsystem::ImportFontAsset(const fs::path &path) {
  return utils::cast<drawing::Font>(ImportAsset(path, {"font"}, [this](const fs::path &path) {
    return ImportFont(path);
  }));
}

std::shared_ptr<audio::AudioBuffer> AssetSubsystem::ImportAudioAsset(
    const fs::path &path) {
  return {};
}

std::shared_ptr<drawing::Texture> AssetSubsystem::ImportTexture(
    const fs::path &path) {

  std::basic_ifstream<unsigned char> stream(
      path, std::ios::in | std::ios::binary);
  const auto eos = std::istreambuf_iterator<unsigned char>();
  auto buffer = std::vector(std::istreambuf_iterator(stream), eos);

  Array<unsigned char> data;

  int texWidth, texHeight, texChannels;

  stbi_uc *pixels = stbi_load(path.string().c_str(), &texWidth, &texHeight,
                              &texChannels, STBI_rgb_alpha);

  if (!pixels) {
    return {};
  }

  data.resize(texWidth * texHeight * 4);

  memcpy(data.data(),pixels,data.size());

  stbi_image_free(pixels);

  

  
  return drawing::Texture::FromMemory(
      data, {static_cast<uint32_t>(texWidth), static_cast<uint32_t>(texHeight),
            1},
      vk::Format::eR8G8B8A8Unorm,
      vk::Filter::eLinear);
}

struct FtContext {
  msdfgen::Point2 position{};
  msdfgen::Shape *shape = nullptr;
  msdfgen::Contour *contour = nullptr;
};

struct GlyphInfo {
  msdfgen::Shape shape;
  FT_GlyphSlotRec_ slot;
  int id;
  std::optional<msdfgen::Bitmap<float, 3>> bmp{};
  double width = 0;
  double height = 0;
  double offsetX = 0;
  double offsetY = 0;
  glm::dvec2 uv{0.0,0.0};
  
  GlyphInfo(const msdfgen::Shape& inShape,FT_GlyphSlotRec_ * inSlot,int inId) {
    shape = inShape;
    slot = *inSlot;
    id = inId;
    if(shape.edgeCount() == 0 && shape.contours.empty()) {
      return;
    }
    const auto bounds = shape.getBounds();
    width = bounds.r - bounds.l;
    height = bounds.t - bounds.b;
    offsetX = bounds.l;
    offsetY = height - bounds.t;
  }

  [[nodiscard]] bool HasGeometry() const {
    if(width == 0 || height == 0) {
      return false;
    }

    return true;
  }

  bool GenerateMsdf() {
    if(!HasGeometry()) {
      return false;
    }
    auto bmpWidth = static_cast<int>(std::ceil(width));
    auto bmpHeight = static_cast<int>(std::ceil(height));
    msdfgen::Bitmap<float, 3> msdf(bmpWidth + 2,bmpHeight + 2);
    const auto projection = msdfgen::Projection{{1.0,1.0},{(offsetX * -1.0) + 1.0,offsetY + 1.0}};
    msdfgen::generateMSDF(msdf,shape,projection,8.0);
    bmp = msdf;

    return true;
  }

  [[nodiscard]] bool Save(const fs::path& path) const {
    if(!bmp.has_value()) {
      return false;
    }
    
    return msdfgen::saveBmp(bmp.value(),path.string().c_str());
  }

  std::shared_ptr<drawing::Texture> ToTexture() {
    if(!bmp.has_value()) {
      return {};
    }

    std::vector<unsigned char> data;
    uint32_t bmpWidth = bmp.value().width();
    uint32_t bmpHeight = bmp.value().height();

    const auto totalPixels = bmpWidth * bmpHeight;

    data.reserve(totalPixels * 4);
//    for(auto i = 0; i < totalPixels; i++) {
//      const auto y = i / bmpWidth;
//      const auto x = i % bmpWidth;
//      const auto pixel = bmp.value()(x,y);
//
//      data.push_back(msdfgen::pixelFloatToByte(pixel[0]));
//      data.push_back(msdfgen::pixelFloatToByte(pixel[1]));
//      data.push_back(msdfgen::pixelFloatToByte(pixel[2]));
//      data.push_back(255);
//    }

      for(auto y = 0; y < bmpHeight; y++) {
          for(auto x = 0; x < bmpWidth; x++) {
              const auto pixel = bmp.value()(x,bmpHeight - (y + 1));

              data.push_back(msdfgen::pixelFloatToByte(pixel[0]));
              data.push_back(msdfgen::pixelFloatToByte(pixel[1]));
              data.push_back(msdfgen::pixelFloatToByte(pixel[2]));
              data.push_back(255);
          }
      }
    
    return drawing::Texture::FromMemory(data,{bmpWidth,bmpHeight,1},vk::Format::eR8G8B8A8Unorm,vk::Filter::eNearest);
  }
};

static msdfgen::Point2 ftPoint2(const FT_Vector &vector) {
  return msdfgen::Point2{
      F26DOT6_TO_DOUBLE(vector.x), F26DOT6_TO_DOUBLE(vector.y)};
}

FT_Error AssetSubsystem::ShapeFromFontGlyph(msdfgen::Shape &shape,
                                            FT_Outline *outline) {
  shape.contours.clear();
  shape.inverseYAxis = false;
  FtContext context{};
  context.shape = &shape;

  FT_Outline_Funcs ftFunctions;

  ftFunctions.move_to = [](const FT_Vector *to, void *user) {
    const auto context = static_cast<FtContext *>(user);
    if (!(context->contour && context->contour->edges.empty()))
      context->contour = &context->shape->addContour();
    context->position = ftPoint2(*to);
    return 0;
  };

  ftFunctions.line_to = [](const FT_Vector *to, void *user) {
    const auto context = static_cast<FtContext *>(user);
    const auto endpoint = ftPoint2(*to);
    if (endpoint != context->position) {
      context->contour->addEdge(
          msdfgen::EdgeHolder(context->position, endpoint));
      context->position = endpoint;
    }
    return 0;
  };

  ftFunctions.conic_to = [](const FT_Vector *control, const FT_Vector *to,
                            void *user) {
    const auto context = static_cast<FtContext *>(user);
    const auto endpoint = ftPoint2(*to);
    if (endpoint != context->position) {
      context->contour->addEdge(
          msdfgen::EdgeHolder(context->position, ftPoint2(*control), endpoint));
      context->position = endpoint;
    }
    return 0;
  };

  ftFunctions.cubic_to = [](const FT_Vector *control1,
                            const FT_Vector *control2, const FT_Vector *to,
                            void *user) {
    const auto context = static_cast<FtContext *>(user);
    const auto endpoint = ftPoint2(*to);
    const auto cross = msdfgen::crossProduct(ftPoint2(*control1) - endpoint,
                                             ftPoint2(*control2) - endpoint);
    if (endpoint != context->position || cross) {
      context->contour->addEdge(msdfgen::EdgeHolder(
          context->position, ftPoint2(*control1), ftPoint2(*control2),
          endpoint));
      context->position = endpoint;
    }
    return 0;
  };

  ftFunctions.shift = 0;
  ftFunctions.delta = 0;

  const FT_Error error = FT_Outline_Decompose(outline, &ftFunctions, &context);
  if (!shape.contours.empty() && shape.contours.back().edges.empty())
    shape.contours.pop_back();

  return error;
}

std::shared_ptr<drawing::Font> AssetSubsystem::ImportFont(
    const fs::path &path) {
  
  FT_Face face;

  if (auto err = FT_New_Face(*_library.get(),
                             path.string().c_str(),
                             0,
                             &face)) {

    GetLogger()->Error("Failed to import font from path: {}",
                       path.string().c_str());
    return {};
  }

  constexpr auto fontSizePixels = 64;
    if (FT_Set_Pixel_Sizes(face,0,fontSizePixels)) {
        utils::verror("Failed to set character size");
    }
  std::vector<GlyphInfo> glyphs;

  for (auto char_index = 32; char_index < 127; char_index++) {

    // char char_ = static_cast<char>(glyph_index);

    const auto glyph_index = FT_Get_Char_Index(face, char_index);
    if (FT_Load_Glyph(
        face, /* handle to face object         */
        glyph_index,
        FT_LOAD_DEFAULT)) {

    }

    msdfgen::Shape shape{};

    if (ShapeFromFontGlyph(shape, &face->glyph->outline)) {
      utils::verror("Failed to create shape");
    }

    shape.normalize();

    msdfgen::edgeColoringSimple(shape, 3.0);

    glyphs.emplace_back(shape, face->glyph,char_index);
  }


  auto font = newObject<drawing::Font>();

  Array<std::shared_ptr<drawing::Texture>> textures;
  std::vector<drawing::Glyph> fontGlyphs;
  const auto hasVertical = FT_HAS_VERTICAL(face);
  const auto exp = 64.0f * fontSizePixels;
  auto i = 32;
  
  float maxAscender = 0.0f;
  auto maxDescender = 0.0f;
  auto fontSizeFloat = static_cast<float>(fontSizePixels);
  for(auto &glyph : glyphs) {
    drawing::Glyph fGlyph{};
    
    fGlyph.id = glyph.id;
    fGlyph.size = glm::fvec2{glyph.width,glyph.height} / fontSizeFloat;
    
    fGlyph.hBearing = glm::fvec2{F26DOT6_TO_DOUBLE(glyph.slot.metrics.horiBearingX),F26DOT6_TO_DOUBLE(glyph.slot.metrics.horiBearingY)} / fontSizeFloat;
    fGlyph.hAdvance = F26DOT6_TO_DOUBLE(glyph.slot.metrics.horiAdvance) / fontSizeFloat;

    maxAscender = std::max(maxAscender,fGlyph.hBearing.y);
    maxDescender = std::max(maxDescender,fGlyph.size.y - fGlyph.hBearing.y);
    
    if(hasVertical) {
      fGlyph.vBearing = glm::fvec2{F26DOT6_TO_DOUBLE(glyph.slot.metrics.vertBearingX),F26DOT6_TO_DOUBLE(glyph.slot.metrics.vertBearingY)} / fontSizeFloat;
      fGlyph.vAdvance = F26DOT6_TO_DOUBLE(glyph.slot.metrics.vertAdvance) / fontSizeFloat;
    }
    
    if(glyph.HasGeometry()) {
        fs::path savePath = "D:\\Github\\vengine\\gen\\test_" + std::to_string(i) + ".png";

//        if(glyph.GenerateMsdf()){
//            if(auto tex = glyph.ToTexture(); tex.IsValid()){
//                tex->Save(savePath);
//            }
//        }

      //textures.push(glyph.ToTexture());
      auto tex = ImportTexture(savePath);
      tex->SetMipMapped(false);
      tex->SetFilter(vk::Filter::eLinear);
      textures.push(tex);
      fGlyph.textureIndex = textures.size() - 1;
    } else {
      fGlyph.textureIndex  = -1;
    }
    
    font->AddGlyph(fGlyph);
    i++;
  }

  font->SetAscender(maxAscender);
  font->SetDescender(maxDescender * -1.0f);
  font->SetTextures(textures);

  return font;
}

std::shared_ptr<audio::AudioBuffer> AssetSubsystem::ImportAudio(
    const fs::path &path) {
  if (!fs::exists(path)) {
    return {};
  }

  // const auto loadedWave = libwave::FromFile(path);
  // auto bytesPerSample = loadedWave->GetBytesPerSample();
  // delete loadedWave;
  return {};
}

String AssetSubsystem::GetName() const {
  return "assets";
}

}
