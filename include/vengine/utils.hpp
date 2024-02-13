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
void vassert(const bool result, fmt::format_string<T...> message,T&&... args) {
  
  if(!result) {
    // std::string * nu = nullptr;
    // nu->at(0);
    auto fmtMessage = fmt::format(message,std::forward<T>(args)...);
    
    const auto errMsg = "Assertion Failed: " + fmtMessage + "\n";
    throw std::runtime_error(errMsg.c_str());
  }
}

std::string createUUID();


}
