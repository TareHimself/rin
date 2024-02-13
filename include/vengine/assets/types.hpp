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

RSTRUCT()
struct VEngineAssetHeader : Serializable {
  RPROPERTY()
  uint32_t version;
  RPROPERTY()
  String type;
  RPROPERTY()
  String name;
  RPROPERTY()
  String meta;

  std::shared_ptr<reflect::wrap::Reflected> GetReflected() const override {
    return reflect::factory::find<VEngineAssetHeader>();
  }

  
  void ReadFrom(Buffer &store) override;

  void WriteTo(Buffer &store) override;
};
}
#endif
