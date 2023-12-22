#pragma once

#include <vector>

namespace vengine {
namespace utils {
  template <typename T>
  void vecRemoveAt(std::vector<T> &vec, size_t pos) {
      std::vector<T>::iterator it = vec.begin();
      std::advance(it, pos);
      vec.erase(it);
  }
}
}
