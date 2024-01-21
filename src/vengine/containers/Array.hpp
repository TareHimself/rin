#pragma once
#include "Buffer.hpp"
#include <vector>
#include <functional>
namespace vengine {


template <typename T>
class Array : public std::vector<T> {
public:

  using std::vector<T>::vector;
  
  // Array();
  // Array(size_t allocSize);
  // Array(const std::initializer_list<T> &other);
  // Array(T* begin,T*end);
  
  
  void Push(T &data);
  void Push(T &&data);
  void Pop();
  void Remove(size_t index);
  int64_t IndexOf(T &data,std::function<bool(T &a,T &b)> equality);

  size_t TypeSize() const;
  
  size_t ByteSize() const;

  Array<T>& operator=(const std::vector<T>& other);
};

#ifndef ARRAY_SERIALIZATION_OPS
#define ARRAY_SERIALIZATION_OPS
template<typename  T>
  Buffer &operator<<(Buffer &out,
                                            const Array<T> &src) {
  const uint64_t numElements = src.size();
  out << numElements;
  out.Write(static_cast<const char *>(static_cast<const void *>(src.data())),src.ByteSize());
  return out;
}

template<typename  T>
Buffer &operator>>(Buffer &in,Array<T> &dst) {
  uint64_t numElements;
  in >> numElements;
  dst.resize(numElements);
  in.Write(static_cast<char *>(static_cast<void *>(dst.data())),dst.ByteSize());
  return in;
}
#endif


template <typename T> void Array<T>::Push(T &data) {
  this->push_back(data);
}

template <typename T> void Array<T>::Push(T &&data) {
  this->push_back(data);
}

template <typename T> void Array<T>::Pop() {
  this->pop_back();
}

template <typename T> void Array<T>::Remove(size_t index) {
  auto it = this->begin();
  std::advance(it, index);
  this->erase(it);
}

template <typename T> int64_t Array<T>::IndexOf(T &data,std::function<bool(T &a,T &b)> equality) {
  for(auto i = 0; i < this->size(); i++) {
    if(equality(data,this->at(i))) {
      return i;
    }
  }

  return -1;
}

template <typename T> size_t Array<T>::TypeSize() const {
  return sizeof(T);
}

template <typename T> size_t Array<T>::ByteSize() const {
  return this->size() * sizeof(T);
}

template <typename T> Array<T> & Array<T>::operator=(
    const std::vector<T> &other) {
  this->clear();
  for(auto item : other) {
    this->push(item);
  }

  return *this;
}

// template <typename T> Array<T> & Array<T>::operator=(
//     const std::initializer_list<T> &other) {
//   
//   Array<T> arr = Array(other.begin(),other.end());
//   
//   return *this;
// }

}
