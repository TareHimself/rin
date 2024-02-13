#pragma once
#include <set>

namespace vengine {
template <typename T,class cmp = std::less<T>>
class Set : public std::set<T,cmp> {
public:
  using std::set<T,cmp>::set;
  void Add(T &item);
  void Add(const T &item);
  void Remove(T &item);
  void Remove(const T &item);
  void Add(T &&item);
  
  void Remove(T &&item);
  Set<T> Clone() const;
};


template <typename T,class cmp = std::less<T>> void Set<T,cmp>::Add(T &item) {
  this->insert(item);
}

template <typename T,class cmp = std::less<T>> void Set<T,cmp>::Add(const T &item) {
  this->insert(item);
}

template <typename T,class cmp = std::less<T>> void Set<T,cmp>::Remove(T &item) {
  this->erase(item);
}

template <typename T,class cmp = std::less<T>> void Set<T,cmp>::Remove(const T &item) {
  this->erase(item);
}

template <typename T,class cmp = std::less<T>> void Set<T,cmp>::Add(T &&item) {
  this->insert(item);
}

template <typename T,class cmp = std::less<T>> void Set<T,cmp>::Remove(T &&item) {
  this->erase(item);
}

template <typename T,class cmp = std::less<T>> Set<T> Set<T,cmp>::Clone() const {
  return {this->begin(),this->end()};
}
}

