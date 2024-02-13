#include <vengine/assets/types.hpp>

namespace vengine::assets {

void VEngineAssetHeader::ReadFrom(Buffer &store) {
  store >> version;
  store >> type;
  store >> name;
  store >> meta;
}

void VEngineAssetHeader::WriteTo(Buffer &store) {
  store << version;
  store << type;
  store << name;
  store << meta;
}
}