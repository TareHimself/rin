#pragma once
#include <string>
#include <vector>
#include <xxhash.h>

namespace vengine {
namespace utils {

  template <class DstType, class SrcType>
  bool isType(const SrcType* src)
  {
    return dynamic_cast<const DstType*>(src) != nullptr;
  }

  std::string hash(const void * data, size_t size, XXH64_hash_t seed = 0);
}
}
