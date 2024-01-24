#include "utils.hpp"


namespace vengine::utils {


void hash() {
  
}

std::string hash(const void * data, const size_t size, const XXH64_hash_t seed) {
  return std::to_string(XXH64(data,size,seed));
}
}
