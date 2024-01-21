#include "utils.hpp"
#include <stdexcept>

namespace vengine::utils {


void hash() {
  
}

std::string hash(const void * data, const size_t size, const XXH64_hash_t seed) {
  return std::to_string(XXH64(data,size,seed));
}

void vassert(const bool result, const std::string &message) {
  if(!result) {
    std::string * nu = nullptr;
    nu->at(0);
    throw std::exception(message.c_str());
  }
}
}
