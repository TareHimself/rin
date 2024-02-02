#include <vengine/assets/Asset.hpp>

namespace vengine {
namespace assets {
VEngineAssetHeader Asset::GetHeader() const {
  return _header;
}

void Asset::SetHeader(const VEngineAssetHeader &header) {
  _header = header;
}
}
}