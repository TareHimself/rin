#include <vengine/io/io.hpp>


namespace vengine::io {

fs::path getCompiledShadersPath() {
  return fs::current_path() / "shaders";
}

fs::path getRawShadersPath() {
  return RAW_SHADERS_PATH;
}

void setRawShadersPath(const fs::path &shadersPath) {
  RAW_SHADERS_PATH = shadersPath;
}

fs::path getRawShaderPath(const std::string &shader) {
  return getRawShadersPath() / shader;
}

std::string readFileAsString(const fs::path &filePath) {
  std::ifstream fileStream(filePath);
  
  std::string fileData(std::istreambuf_iterator<char>{fileStream},{});

  fileStream.close();
  return fileData;
}

void writeStringToFile(const fs::path &filePath,
                       const std::string &data) {
  std::ofstream out(filePath);
  out <<  data;
  out.close();
}


}
