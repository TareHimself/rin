// META_IGNORE
#pragma once
#include "Type.hpp"

#include <any>

namespace aerox::meta {

class Value {
  Type _type;
  std::any _data;

public:
  template <typename T>
  Value(const T& data);

  Type GetType() const;

  template <typename T>
  operator T ();

  template <typename T>
  operator T () const;

  Value();
};
//
// template <typename T>
// T *Value::GetPtr() const {
//   return static_cast<T *>(_data);
// }
//
// template <typename T>
// T *Value::GetPtr() {
//   if(GetType().GetFlags().Has(ePointer)) {
//     
//   }
//   return static_cast<T *>(_data);
// }

// template <typename T>
// Value::operator T() {
//   return *GetPtr<T>();
// }
//
// template <typename T>
// Value::operator T() const {
//   return *GetPtr<T>();
// }

template <typename T> Value::Value(const T &data) {
  _data = data;
  _type = Type::Infer<T>();
}

template <typename T>
Value::operator T () {
  // if(GetType() != Type::Infer<T>()) {
  //   throw std::runtime_error("Type Mismatch");
  // }
  return std::any_cast<T>(_data);
}

template <typename T>
Value::operator T () const {
  // if(GetType() != Type::Infer<T>()) {
  //   throw std::runtime_error("Type Mismatch");
  // }
  return std::any_cast<T>(_data);
}
}
