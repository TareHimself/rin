#include "utils.hpp"
#include <stdexcept>

namespace vengine {
namespace utils {


void hash() {
  
}

std::string hash(const void * data, size_t size,  XXH64_hash_t seed) {
  return std::to_string(XXH64(data,size,seed));
}
}
}
