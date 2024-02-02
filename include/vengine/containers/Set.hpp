#pragma once
#include <set>

namespace vengine {
template <typename T>
class Set : public std::set<T> {
public:
  using std::set<T>::set;
  void Add(T &item);
  void Add(const T &item);
  void Remove(T &item);
  void Remove(const T &item);
  void Add(T &&item);
  
  void Remove(T &&item);
  Set<T> Clone();
};


template <typename T> void Set<T>::Add(T &item) {
  this->insert(item);
}

template <typename T> void Set<T>::Add(const T &item) {
  this->insert(item);
}

template <typename T> void Set<T>::Remove(T &item) {
  this->erase(item);
}

template <typename T> void Set<T>::Remove(const T &item) {
  this->erase(item);
}

template <typename T> void Set<T>::Add(T &&item) {
  this->insert(item);
}

template <typename T> void Set<T>::Remove(T &&item) {
  this->erase(item);
}

template <typename T> Set<T> Set<T>::Clone() {
  return {this->begin(),this->end()};
}
}

