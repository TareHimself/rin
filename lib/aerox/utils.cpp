#include "aerox/utils.hpp"
#include "xxhash.h"
#define UUID_SYSTEM_GENERATOR
#include "uuid.h"
namespace aerox::utils {

std::string hash(const void * data, const size_t size, const XXH64_hash_t seed) {
  return std::to_string(XXH64(data,size,seed));
}

std::string uuid() {
  return to_string(uuids::uuid_system_generator{}());
}
}
