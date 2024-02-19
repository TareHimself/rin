#ifndef VENGINE_CONTAINERS_BUFFER
#define VENGINE_CONTAINERS_BUFFER
#include <fstream>
#include <vector>
#include <vengine/fs.hpp>
#ifndef VENGINE_SIMPLE_BUFFER_SERIALIZER
#define VENGINE_SIMPLE_BUFFER_SERIALIZER(Buffer, Type) \
inline Buffer &operator<<(Buffer &dst, const Type &src) { \
dst.Write(static_cast<const char *>(static_cast<const void *>(&src)), sizeof(Type)); \
return dst; \
} \
inline Buffer &operator>>(Buffer &src, Type &dst) { \
src.Read(static_cast<char *>(static_cast<void *>(&dst)), sizeof(Type)); \
return src; \
}
#endif

namespace vengine {
class Serializable;
}

namespace vengine {
class Buffer {

public:
  virtual ~Buffer() = default;
  virtual Buffer& Write(const char* src,size_t byteSize) = 0;
  
  virtual Buffer& Read(char * dst,size_t byteSize) = 0;

  virtual Buffer& Skip(size_t byteSize);
  
  virtual size_t size() const = 0;

  Buffer& operator<<(Serializable& src);
  Buffer& operator>>(Serializable& dst);

  Buffer& operator<<(Buffer& src);
  Buffer& operator>>(Buffer& dst);

  // Buffer& operator<<(const uint8_t& src);
  // Buffer& operator>>(uint8_t& dst);
  //
  // Buffer& operator<<(const uint16_t& src);
  // Buffer& operator>>(uint16_t& dst);
  //
  // Buffer& operator<<(const uint32_t& src);
  // Buffer& operator>>(uint32_t& dst);
  //
  // Buffer& operator<<(const uint64_t& src);
  // Buffer& operator>>(uint64_t& dst);
  //
  // Buffer& operator<<(const int& src);
  // Buffer& operator>>(int& dst);
  //
  // Buffer& operator<<(const float& src);
  // Buffer& operator>>(float& dst);
  //
  // Buffer& operator<<(const double& src);
  // Buffer& operator>>(double& dst);
  
};

VENGINE_SIMPLE_BUFFER_SERIALIZER(Buffer,uint8_t);
VENGINE_SIMPLE_BUFFER_SERIALIZER(Buffer,uint16_t);
VENGINE_SIMPLE_BUFFER_SERIALIZER(Buffer,uint32_t);
VENGINE_SIMPLE_BUFFER_SERIALIZER(Buffer,uint64_t);
VENGINE_SIMPLE_BUFFER_SERIALIZER(Buffer,int);
VENGINE_SIMPLE_BUFFER_SERIALIZER(Buffer,float);
VENGINE_SIMPLE_BUFFER_SERIALIZER(Buffer,double);
// #ifndef BUFFER_SERIALIZATION_OPS
// #define BUFFER_SERIALIZATION_OPS
// VENGINE_SIMPLE_BUFFER_SERIALIZER(Buffer,uint8_t);
// VENGINE_SIMPLE_BUFFER_SERIALIZER(Buffer,uint16_t);
// VENGINE_SIMPLE_BUFFER_SERIALIZER(Buffer,uint32_t);
// VENGINE_SIMPLE_BUFFER_SERIALIZER(Buffer,uint64_t);
// VENGINE_SIMPLE_BUFFER_SERIALIZER(Buffer,int);
// VENGINE_SIMPLE_BUFFER_SERIALIZER(Buffer,float);
// VENGINE_SIMPLE_BUFFER_SERIALIZER(Buffer,double);
// #endif

class MemoryBuffer : public Buffer {
  std::vector<char> _data;
public:
  MemoryBuffer();
  MemoryBuffer(const std::vector<char> &data);
  virtual void clear();
  Buffer& Read(char *dst, size_t byteSize) override;
  Buffer& Write(const char *src, size_t byteSize) override;
  size_t size() const override;
};

class FileBuffer : public  Buffer {
protected:
  size_t _dataSize = 0;
public:
  size_t size() const override;
  virtual bool isOpen() const = 0;
  virtual void close() = 0;
};

class InFileBuffer : public FileBuffer {
  std::ifstream _stream;
public:
  InFileBuffer(const fs::path &filePath);

  bool isOpen() const override;
  void close() override;
  Buffer& Read(char *dst, size_t byteSize) override;
  Buffer& Write(const char *src, size_t byteSize) override;
};

class OutFileBuffer : public FileBuffer {
  std::ofstream _stream;
public:
  OutFileBuffer(const fs::path &filePath);

  bool isOpen() const override;
  void close() override;
  Buffer& Read(char *dst, size_t byteSize) override;
  Buffer& Write(const char *src, size_t byteSize) override;
};

}
#endif