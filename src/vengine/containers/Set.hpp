#pragma once
#include <set>

namespace vengine {
template <typename T>
class Set : public std::set<T> {
public:
  void Add(T &item);
  void Remove(T &item);
};


template <typename T> void Set<T>::Add(T &item) {
  this->emplace(item);
}

template <typename T> void Set<T>::Remove(T &item) {
  this->erase(item);
}
}

