#pragma once
#include "Buffer.hpp"
#include <string>

namespace vengine {
class String : public std::string {
public:
  using std::string::string;

  String &operator =(const std::string &other) {
    clear();
    resize(other.size());
    memcpy(data(),other.data(),other.size());
    
    return *this;
  }
};

#ifndef STRING_SERIALIZATION_OPS
#define STRING_SERIALIZATION_OPS

inline Buffer &operator<<(Buffer &out,
                                  const String &src) {
  const uint64_t stringSize = src.length();
  out << stringSize;
  out.Write(src.data(),stringSize);
  return out;
}

inline Buffer &operator>>(Buffer &in,
                                  String &dst) {
  uint64_t stringSize;
  in >> stringSize;
  dst.resize(stringSize);
  in.Read(dst.data(),stringSize);

  return in;
}
#endif
}
