#ifndef VENGINE_ASSETS_TYPES
#define VENGINE_ASSETS_TYPES
#include "vengine/containers/Serializable.hpp"
#include "vengine/containers/String.hpp"
#include "vengine/drawing/types.hpp"
#include <cstdint>

namespace vengine::assets {

namespace types {
const String MESH = "MESH";
const String TEXTURE = "TEXTURE";
const String FONT = "FONT";
}

struct VEngineAssetHeader : Serializable {
  uint32_t version;
  String type;
  String name;
  String meta;

  String GetSerializeId() override;

  void ReadFrom(Buffer &store) override;

  void WriteTo(Buffer &store) override;
};
}
#endif
