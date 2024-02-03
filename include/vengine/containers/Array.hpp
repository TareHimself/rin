﻿#pragma once
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
  
  
  void Push(T &data);
  void Push(T &&data);
  void Push(const T &data);
  void Pop();
  void Remove(size_t index);
  Array<T> Clone();
  std::optional<uint64_t> IndexOf(T data);
  std::optional<uint64_t> IndexOf(T data,std::function<bool(T &a,T &b)> comparator);

  size_t TypeSize() const;
  
  size_t ByteSize() const;

  Array<T>& operator=(const std::vector<T>& other);
};

#ifndef VENGINE_SIMPLE_ARRAY_SERIALIZER
#define VENGINE_SIMPLE_ARRAY_SERIALIZER(Buffer, Type) \
inline Buffer &operator<<(Buffer &dst,const Array<Type> &src) { \
  const uint64_t numElements = src.size(); \
  dst << numElements; \
  dst.Write(static_cast<const char *>(static_cast<const void *>(src.data())),src.ByteSize()); \
  return dst; \
} \
inline Buffer &operator>>(Buffer &src,Array<Type> &dst) { \
  uint64_t numElements; \
  src >> numElements; \
  dst.resize(numElements); \
  src.Read(static_cast<char *>(static_cast<void *>(dst.data())),dst.ByteSize()); \
  return src; \
}
#endif

VENGINE_SIMPLE_ARRAY_SERIALIZER(Buffer,unsigned char);
VENGINE_SIMPLE_ARRAY_SERIALIZER(Buffer,uint16_t);
VENGINE_SIMPLE_ARRAY_SERIALIZER(Buffer,uint32_t);
VENGINE_SIMPLE_ARRAY_SERIALIZER(Buffer,uint64_t);
VENGINE_SIMPLE_ARRAY_SERIALIZER(Buffer,int);
VENGINE_SIMPLE_ARRAY_SERIALIZER(Buffer,float);
VENGINE_SIMPLE_ARRAY_SERIALIZER(Buffer,double);

template <typename T> void Array<T>::Push(T &data) {
  this->push_back(data);
}

template <typename T> void Array<T>::Push(T &&data) {
  this->push_back(data);
}

template <typename T> void Array<T>::Push(const T &data) {
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

template <typename T> Array<T> Array<T>::Clone() {
  Array<T> result;
  result.resize(this->size());
  memcpy(result.data(),this->data(),result.ByteSize());
  return result;
}

template <typename T> std::optional<uint64_t> Array<T>::IndexOf(T data) {
  for(auto i = 0; i < this->size(); i++) {
    if(data == this->at(i)) {
      return i;
    }
  }

  return std::nullopt;
}

template <typename T> std::optional<uint64_t> Array<T>::IndexOf(T data,std::function<bool(T &a,T &b)> comparator) {
  for(auto i = 0; i < this->size(); i++) {
    if(comparator(data,this->at(i))) {
      return i;
    }
  }

  return std::nullopt;
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