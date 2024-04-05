#pragma once
#include "Type.hpp"

namespace aerox::meta {
class Reference {

protected:
  void * _data = nullptr;
  Type _type{};
public:

  Reference();

  template<typename T>
  Reference(const T& data);

  template<typename T>
  operator T&();

  template<typename T>
  operator T&() const;

  Type GetType() const;

  // template<typename T>
  // T * GetPtr();
  //
  // template<typename T>
  // T * GetPtr() const;
  
};

template <typename T> Reference::Reference(const T &data) {
  _type = Type::Infer<T>();
  _data = (void *)(&data);
}

template <typename T> Reference::operator T&() {
  // if(Type::Infer<T>() != _type) {
  //   throw std::runtime_error("Type Mismatch");
  // }

  return *static_cast<T *>(_data);
}

template <typename T> Reference::operator T&() const {
  // if(Type::Infer<T>() != _type) {
  //   throw std::runtime_error("Type Mismatch");
  // }

  return *static_cast<T *>(_data);
}

// template <typename T> T * Reference::GetPtr() {
// return static_cast<T *>(_data);
// }
//
// template <typename T> T * Reference::GetPtr() const {
//   return static_cast<T *>(_data);
// }
}
