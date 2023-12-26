#pragma once
#include <set>

namespace vengine {
template <typename T>
class Set : public std::set<T> {
public:
  void add(T &item);
  void remove(T &item);
};


template <typename T> void Set<T>::add(T &item) {
  this->emplace(item);
}

template <typename T> void Set<T>::remove(T &item) {
  this->erase(item);
}
}

