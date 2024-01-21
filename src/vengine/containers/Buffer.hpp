#ifndef VENGINE_CONTAINERS_BUFFER
#define VENGINE_CONTAINERS_BUFFER
#include <fstream>
#include <vector>

#ifndef SIMPLE_BUFFER_SERIALIZER
#define SIMPLE_BUFFER_SERIALIZER(Buffer, Type) \
inline Buffer &operator<<(Buffer &out, const Type &src) { \
out.Write(static_cast<const char *>(static_cast<const void *>(&src)), sizeof(Type)); \
return out; \
} \
inline Buffer &operator>>(Buffer &in, Type &dst) { \
in.Write(static_cast<char *>(static_cast<void *>(&dst)), sizeof(Type)); \
return in; \
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

#ifndef BUFFER_SERIALIZATION_OPS
#define BUFFER_SERIALIZATION_OPS
SIMPLE_BUFFER_SERIALIZER(Buffer,uint8_t);
SIMPLE_BUFFER_SERIALIZER(Buffer,uint16_t);
SIMPLE_BUFFER_SERIALIZER(Buffer,uint32_t);
SIMPLE_BUFFER_SERIALIZER(Buffer,uint64_t);
SIMPLE_BUFFER_SERIALIZER(Buffer,int);
SIMPLE_BUFFER_SERIALIZER(Buffer,float);
SIMPLE_BUFFER_SERIALIZER(Buffer,double);
#endif

class MemoryBuffer : public Buffer {
  std::vector<char> _data;
public:
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
  InFileBuffer(const std::filesystem::path &filePath);

  bool isOpen() const override;
  void close() override;
  Buffer& Read(char *dst, size_t byteSize) override;
  Buffer& Write(const char *src, size_t byteSize) override;
};

class OutFileBuffer : public FileBuffer {
  std::ofstream _stream;
public:
  OutFileBuffer(const std::filesystem::path &filePath);

  bool isOpen() const override;
  void close() override;
  Buffer& Read(char *dst, size_t byteSize) override;
  Buffer& Write(const char *src, size_t byteSize) override;
};

}
#endif