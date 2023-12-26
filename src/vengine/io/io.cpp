#include <vengine/io/io.hpp>


namespace vengine {
namespace io {

std::filesystem::path getCompiledShadersPath() {
  return std::filesystem::current_path() / "shaders";
}

std::filesystem::path getRawShadersPath() {
  return _rawShadersPath;
}

void setRawShadersPath(const std::filesystem::path &shadersPath) {
  _rawShadersPath = shadersPath;
}

std::filesystem::path getRawShaderPath(const std::string &shader) {
  return getRawShadersPath() / shader;
}

std::string readFileAsString(const std::filesystem::path &filePath) {
  std::ifstream fileStream(filePath);
  
  std::string fileData(std::istreambuf_iterator<char>{fileStream},{});

  fileStream.close();
  return fileData;
}

void writeStringToFile(const std::filesystem::path &filePath,
    const std::string &data) {
  std::ofstream out(filePath);
  out <<  data;
  out.close();
}


}
}