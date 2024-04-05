#pragma once
#include "aerox/containers/Array.hpp"
#include <fstream>
#include <string>
#include <aerox/fs.hpp>

namespace aerox::io {
inline fs::path RAW_SHADERS_PATH = "";
fs::path getCompiledShadersPath();
fs::path getRawShadersPath();
void setRawShadersPath(const fs::path &shadersPath);

fs::path getRawShaderPath(const std::string &shader);

std::string readFileAsString(const fs::path &filePath);

void writeStringToFile(const fs::path &filePath,const std::string &data);

template<typename  T>
Array<T> readFile(const fs::path &filePath) {
  std::ifstream fileStream(filePath, std::ios::ate | std::ios::binary);

  if(!fileStream.is_open()) {
    throw std::runtime_error("Failed to open file: " + filePath.string());
  }

  const size_t fileSize = static_cast<size_t>(fileStream.tellg());

  // Calculate the number of elements based on file size
  const size_t numElements = fileSize / sizeof(T);

  //put file cursor at beginning
  fileStream.seekg(0);
      
  Array<T> fileData(numElements);

  fileStream.read(reinterpret_cast<char*>(fileData.data()),fileSize);

  fileStream.close();
      
  return fileData;
      
}

    

}
