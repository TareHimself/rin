#pragma once
#include "TContainer.hpp"

template<typename T>
class TCContainer : public TContainer<T>{
public:
  void Add(const T &data) override;
  void Add(T &&data) override;
  size_t Remove(size_t index, size_t count) override;
  void Reserve(const size_t &size) override;
  size_t Size() const override;
  bool Empty() const override;
  std::optional<size_t> IndexOf(T &&data) override;
  T & operator[](const size_t &index) override;
  T & operator[](const size_t &index) const override;
  T & Get(const size_t &idx) override;
  T * Find(const std::function<bool(T &)> &callback) override;
  T * Find(const std::function<bool(T &, size_t)> &callback) override;
  std::optional<size_t>
  FindIndex(const std::function<bool(T &)> &callback) override;
  std::optional<size_t> FindIndex(
      const std::function<bool(T &, size_t)> &callback) override;

protected:
  T * _data = nullptr;
  size_t _size = 0;
  size_t _allocatedSize = 0;
  
  void CheckSize();
public:
  
};

template <typename T> void TCContainer<T>::Add(const T &data) {
  CheckSize();
  _data[_size] = data;
  _size++;
}

template <typename T> void TCContainer<T>::Add(T &&data) {
  CheckSize();
  _data[_size] = data;
  _size++;
}

template <typename T> size_t TCContainer<T>::Remove(size_t index, size_t count) {
  if(index >= Size()) {
    throw std::runtime_error("Index out of range");
  }

  auto start = index;
  auto stop = std::min(index + count,Size());
  if(start == stop) {
    return 0;
  }

  auto diff = stop - start;
  
  std::copy_n(_data + stop,diff,_data + start);

  _size -= diff;
  
  return diff;
}

template <typename T> void TCContainer<T>::Reserve(const size_t &size) {
  auto newArr = new T[size];

  if(_data == nullptr) {
    _data = newArr;
    
  }else {
    std::copy_n(_data,_size,newArr);
    delete[] _data;
  }
  
  _allocatedSize = size;
}

template <typename T> size_t TCContainer<T>::Size() const {
  return _size;
}

template <typename T> bool TCContainer<T>::Empty() const {
  return Size() == 0;
}

template <typename T> std::optional<size_t> TCContainer<T>::IndexOf(T &&data) {
  for(auto i = 0; i < Size(); i++) {
    if(transform(this->Get(i))) {
      return i;
    }
  }

  return {};
}

template <typename T> T & TCContainer<T>::operator[](const size_t &index) {
}

template <typename T> T & TCContainer<T>::operator[
](const size_t &index) const {
}

template <typename T> T & TCContainer<T>::Get(const size_t &idx) {
}

template <typename T> T * TCContainer<T>::Find(
    const std::function<bool(T &)> &callback) {
}

template <typename T> T * TCContainer<T>::Find(
    const std::function<bool(T &, size_t)> &callback) {
}

template <typename T> std::optional<size_t> TCContainer<T>::FindIndex(
    const std::function<bool(T &)> &callback) {
}

template <typename T> std::optional<size_t> TCContainer<T>::FindIndex(
    const std::function<bool(T &, size_t)> &callback) {
}

template <typename T> void TCContainer<T>::CheckSize() {
}
