#pragma once
#include <fstream>
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
  
  friend std::ofstream &operator <<(std::ofstream &out, const String &a) {
    uint64_t str_size = a.size();
    out.write(reinterpret_cast<char *>(&str_size),sizeof(str_size));
    out.write(a.data(),str_size);
    return out;
  }

  friend std::ifstream &operator >>(std::ifstream &in, String &a) {
    uint64_t elementNum;
    in.read(reinterpret_cast<char *>(&elementNum),sizeof(a.size()));
    a.resize(elementNum);
    in.read(a.data(),a.size());
    return in;
  }

};
}
