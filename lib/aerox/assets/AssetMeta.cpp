#include <aerox/assets/AssetMeta.hpp>

namespace aerox::assets {

void AssetMeta::ReadFrom(Buffer &store) {
  store << version;
  store << type;
  store << id;
  store << tags;
}

void AssetMeta::WriteTo(Buffer &store) {
  store >> version;
  store >> type;
  store >> id;
  store >> tags;
}
}