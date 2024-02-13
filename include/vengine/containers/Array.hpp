#pragma once
#include "Buffer.hpp"
#include <vector>
#include <functional>
#include <optional>

namespace vengine {


template <typename T>
class Array : public std::vector<T> {
public:
  using std::vector<T>::vector;

  // Array();
  // Array(size_t allocSize);
  // Array(const std::initializer_list<T> &other);
  // Array(T* begin,T*end);


  void push(T &data);
  void push(T &&data);
  void push(const T &data);
  void pop();
  void remove(size_t index);

  std::optional<T> try_index(size_t index) const;

  template <typename E>
  Array<E> map(
      const std::function<E(size_t index, const T &item)> &transform) const;
  Array clone();
  std::optional<uint64_t> index_of(T data);
  std::optional<uint64_t> index_of(
      T data, std::function<bool(T &a, T &b)> comparator);

  size_t type_size() const;

  size_t byte_size() const;

  Array<T> &operator=(const std::vector<T> &other);
};

#ifndef VENGINE_SIMPLE_ARRAY_SERIALIZER
#define VENGINE_SIMPLE_ARRAY_SERIALIZER(Buffer, Type) \
inline Buffer &operator<<(Buffer &dst,const Array<Type> &src) { \
  const uint64_t numElements = src.size(); \
  dst << numElements; \
  dst.Write(static_cast<const char *>(static_cast<const void *>(src.data())),src.byte_size()); \
  return dst; \
} \
inline Buffer &operator>>(Buffer &src,Array<Type> &dst) { \
  uint64_t numElements; \
  src >> numElements; \
  dst.resize(numElements); \
  src.Read(static_cast<char *>(static_cast<void *>(dst.data())),dst.byte_size()); \
  return src; \
}
#endif

VENGINE_SIMPLE_ARRAY_SERIALIZER(Buffer, unsigned char);
VENGINE_SIMPLE_ARRAY_SERIALIZER(Buffer, uint16_t);
VENGINE_SIMPLE_ARRAY_SERIALIZER(Buffer, uint32_t);
VENGINE_SIMPLE_ARRAY_SERIALIZER(Buffer, uint64_t);
VENGINE_SIMPLE_ARRAY_SERIALIZER(Buffer, int);
VENGINE_SIMPLE_ARRAY_SERIALIZER(Buffer, float);
VENGINE_SIMPLE_ARRAY_SERIALIZER(Buffer, double);

template <typename T> void Array<T>::push(T &data) {
  this->push_back(data);
}

template <typename T> void Array<T>::push(T &&data) {
  this->push_back(data);
}

template <typename T> void Array<T>::push(const T &data) {
  this->push_back(data);
}


template <typename T> void Array<T>::pop() {
  this->pop_back();
}

template <typename T> void Array<T>::remove(size_t index) {
  auto it = this->begin();
  std::advance(it, index);
  this->erase(it);
}

template <typename T> std::optional<T> Array<T>::try_index(size_t index) const {
  if(index >= this->size()) {
    return {};
  }

  return this->at(index);
}

template <typename T> template <typename E> Array<E> Array<T>::map(
    const std::function<E(size_t index, const T &item)> &transform) const {
  Array<E> result;
  result.resize(this->size());
  for (auto i = 0; i < this->size(); i++) {
    result.push_back(transform(i, this->at(i)));
  }

  return result;
}

template <typename T> Array<T> Array<T>::clone() {
  Array<T> result;
  result.resize(this->size());
  memcpy(result.data(), this->data(), result.byte_size());
  return result;
}

template <typename T> std::optional<uint64_t> Array<T>::index_of(T data) {
  for (auto i = 0; i < this->size(); i++) {
    if (data == this->at(i)) {
      return i;
    }
  }

  return std::nullopt;
}

template <typename T> std::optional<uint64_t> Array<T>::index_of(
    T data, std::function<bool(T &a, T &b)> comparator) {
  for (auto i = 0; i < this->size(); i++) {
    if (comparator(data, this->at(i))) {
      return i;
    }
  }

  return std::nullopt;
}

template <typename T> size_t Array<T>::type_size() const {
  return sizeof(T);
}

template <typename T> size_t Array<T>::byte_size() const {
  return this->size() * sizeof(T);
}

template <typename T> Array<T> &Array<T>::operator=(
    const std::vector<T> &other) {
  this->clear();
  for (auto item : other) {
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
