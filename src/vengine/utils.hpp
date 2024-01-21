#pragma once
#include <string>
#include <xxhash.h>
#include <uuid.h>
namespace vengine::utils {

template <class DstType, class SrcType>
bool isType(const SrcType* src)
{
  return dynamic_cast<const DstType*>(src) != nullptr;
}

std::string hash(const void * data, size_t size, XXH64_hash_t seed = 0);

void vassert(bool result, const std::string &message);
}
