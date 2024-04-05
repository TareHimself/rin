#pragma once
#include "log.hpp"
#include <string>
#include <fmt/format.h>
namespace aerox::utils {

// template <typename T,typename E>
// inline bool operator==(const std::shared_ptr<T> &b,const std::shared_ptr<E> &a) {
//   return b.get() == a.get();
// }

template <class DstType, class SrcType>
bool isType(const SrcType* src)
{
  return dynamic_cast<const DstType*>(src) != nullptr;
}

std::string hash(const void * data, size_t size, uint64_t seed = 0);

template <typename... T>
void verror [[noreturn]] (const fmt::format_string<T...>& message,T&&... args) {
  auto fmtMessage = fmt::format(message,std::forward<T>(args)...);
  
  throw std::runtime_error(fmtMessage.c_str());
}

template <typename... T>
void vassert(const bool result, const fmt::format_string<T...>& message,T&&... args) {
  
  if(!result) {
    const std::string fmtMessage = fmt::format(message,std::forward<T>(args)...);
    throw std::runtime_error(std::string(std::string("Assertion Failed: ") + "\n" + fmtMessage).c_str());
  }
}

std::string uuid();

template <typename T>
bool nearlyEqual(T a,T b,T tolerance) {
  return std::abs(a - b) < tolerance;
}

template <typename To,typename From>std::shared_ptr<To> cast(const std::shared_ptr<From>& ptr) {
  return std::dynamic_pointer_cast<To,From>(ptr);
}

template <typename To,typename From>
std::shared_ptr<To> castStatic(const std::shared_ptr<From>& ptr) {
  return std::static_pointer_cast<To,From>(ptr);
}
}
