#pragma once
#include <string>
#include <fmt/format.h>
namespace vengine::utils {


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
void verror [[noreturn]] (fmt::format_string<T...> message,T&&... args) {
  auto fmtMessage = fmt::format(message,std::forward<T>(args)...);
  
  throw std::runtime_error(fmtMessage.c_str());
}

template <typename... T>
void vassert(const bool result, fmt::format_string<T...> message,T&&... args) {
  
  if(!result) {
    const std::string fmtMessage = fmt::format(message,std::forward<T>(args)...);
    
    throw std::runtime_error(std::string("Assertion Failed: " + fmtMessage + "\n").c_str());
  }
}

std::string createUUID();

template <typename T>
bool nearlyEqual(T a,T b,T tolerance) {
  return std::abs(a - b) < tolerance;
}
}
