#include "vengine/utils.hpp"
#include "xxhash.h"
#define UUID_SYSTEM_GENERATOR
#include "uuid.h"
namespace vengine::utils {

std::string hash(const void * data, const size_t size, const XXH64_hash_t seed) {
  return std::to_string(XXH64(data,size,seed));
}

std::string createUUID() {
  return to_string(uuids::uuid_system_generator{}());
}
}
