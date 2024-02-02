#include <vengine/containers/Buffer.hpp>
#include <vengine/containers/Serializable.hpp>
#include "vengine/utils.hpp"
#include <filesystem>

namespace vengine {
Buffer & Buffer::Skip(const size_t byteSize) {
  if(byteSize <= 0) {
    return *this;
  }
  char * data = new char[byteSize];
  Read(data,byteSize);
  delete[] data;
  return *this;
}

Buffer & Buffer::operator<<(Serializable &src) {
  auto data = MemoryBuffer();
  src.WriteTo(data);
  *this << src.GetSerializeId();
  const auto size = static_cast<uint64_t>(data.size());
  *this << size;
  *this << data;
  data.clear();
  return *this;
}

Buffer & Buffer::operator<<(Buffer &src){
  std::vector<char> temp;
  temp.resize(src.size());
  src.Read(temp.data(),temp.size());
  Write(temp.data(),temp.size());
  temp.clear();
  return  *this;
}

Buffer & Buffer::operator>>(Buffer &dst){
  std::vector<char> temp;
  temp.resize(size());
  Read(temp.data(),size());
  dst.Write(temp.data(),temp.size());
  temp.clear();
  return  *this;
}

// Buffer & Buffer::operator<<(const uint8_t &src) {
//   Write(reinterpret_cast<const char *>(&src),sizeof(uint8_t));
//   return *this;
// }
//
// Buffer & Buffer::operator>>(uint8_t &dst) {
//   Read(reinterpret_cast<char *>(&dst),sizeof(uint8_t));
//   return *this;
// }
//
// Buffer & Buffer::operator<<(const uint16_t &src) {
//   Write(reinterpret_cast<const char *>(&src),sizeof(uint16_t));
//   return *this;
// }
//
// Buffer & Buffer::operator>>(uint16_t &dst) {
//   Read(reinterpret_cast<char *>(&dst),sizeof(uint16_t));
//   return *this;
// }
//
// Buffer & Buffer::operator<<(const uint32_t &src) {
//   Write(reinterpret_cast<const char *>(&src),sizeof(uint32_t));
//   return *this;
// }
//
// Buffer & Buffer::operator>>(uint32_t &dst) {
//   Read(reinterpret_cast<char *>(&dst),sizeof(uint32_t));
//   return *this;
// }
//
// Buffer & Buffer::operator<<(const uint64_t &src) {
//   Write(reinterpret_cast<const char *>(&src),sizeof(uint64_t));
//   return *this;
// }
//
// Buffer & Buffer::operator>>(uint64_t &dst) {
//   Read(reinterpret_cast<char *>(&dst),sizeof(uint64_t));
//   return *this;
// }
//
// Buffer & Buffer::operator<<(const int &src) {
//   Write(reinterpret_cast<const char *>(&src),sizeof(int));
//   return *this;
// }
//
// Buffer & Buffer::operator>>(int &dst) {
//   Read(reinterpret_cast<char *>(&dst),sizeof(int));
//   return *this;
// }
//
// Buffer & Buffer::operator<<(const float &src) {
//   Write(reinterpret_cast<const char *>(&src),sizeof(float));
//   return *this;
// }
//
// Buffer & Buffer::operator>>(float &dst) {
//   Read(reinterpret_cast<char *>(&dst),sizeof(float));
//   return *this;
// }
//
// Buffer & Buffer::operator<<(const double &src) {
//   Write(reinterpret_cast<const char *>(&src),sizeof(double));
//   return *this;
// }
//
// Buffer & Buffer::operator>>(double &dst) {
//   Read(reinterpret_cast<char *>(&dst),sizeof(double));
//   return *this;
// }

Buffer & Buffer::operator>>(Serializable &dst) {
  String id;
  *this >> id;
  utils::vassert(id == dst.GetSerializeId(),"Serialization ID Mismatch");
  uint64_t dataSize;
  *this >> dataSize;
  dst.ReadFrom(*this);
  return *this;
}

Buffer & MemoryBuffer::Write(const char *src, const size_t byteSize) {
  _data.insert(_data.end(),src,src + byteSize);
  return *this;
}

MemoryBuffer::MemoryBuffer() {
}

MemoryBuffer::MemoryBuffer(const std::vector<char> &data) {
  _data = data;
}

void MemoryBuffer::clear() {
  _data.clear();
}

Buffer & MemoryBuffer::Read(char *dst, const size_t byteSize) {
  const auto myDataSize = _data.size();
  const auto readSize = std::min(byteSize,myDataSize);

  std::copy_n(_data.begin(),readSize,dst);

  _data.erase(_data.begin(),_data.begin() + readSize);
  return *this;
}

size_t MemoryBuffer::size() const {
  return _data.size();
}

size_t FileBuffer::size() const {
  return _dataSize;
}

InFileBuffer::InFileBuffer(const std::filesystem::path &filePath) {
  _stream.open(filePath.c_str(), std::ios::binary | std::ios::in);
  if(InFileBuffer::isOpen()) {
    _dataSize = std::filesystem::file_size(filePath);
  }
}

bool InFileBuffer::isOpen() const {
  return _stream.is_open();
}

void InFileBuffer::close() {
  _stream.close();
}


Buffer & InFileBuffer::Read(char *dst, size_t byteSize) {
  _stream.read(dst,byteSize);
  _dataSize -= byteSize;
  return *this;
}

Buffer & InFileBuffer::Write(const char *src, size_t byteSize) {
  throw std::exception("Cannot Write To InFileBuffer");
  return *this;
}

OutFileBuffer::OutFileBuffer(const std::filesystem::path &filePath) {
  _stream.open(filePath.c_str(), std::ios::binary | std::ios::out);
  _dataSize = 0;
}

bool OutFileBuffer::isOpen() const {
  return _stream.is_open();
}

void OutFileBuffer::close() {
  _stream.close();
}

Buffer & OutFileBuffer::Read(char *dst, size_t byteSize) {
  throw std::exception("Cannot Read From OutFileBuffer");
  return *this;
}

Buffer & OutFileBuffer::Write(const char *src, size_t byteSize) {
  _stream.write(src,byteSize);
  _dataSize += byteSize;
  return *this;
}


}
