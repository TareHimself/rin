#pragma once
#include "aerox/EngineSubsystem.hpp"
#include "gen/io/IoSubsystem.gen.hpp"
#include "aerox/containers/Array.hpp"
#include "aerox/containers/TFlags.hpp"
namespace aerox::io {

enum EDialogFlags {
  DF_SELECT_FILES = flagField(0),
  DF_SELECT_MULTIPLE = flagField(1)
};
META_TYPE()
class IoSubsystem : public EngineSubsystem{

protected:
  fs::path _rawShadersPath;
public:

  META_BODY()
  
  String GetName() const override;

  void OnInit(Engine *owner) override;
  void OnDestroy() override;

  fs::path GetApplicationPath() const;
  fs::path GetCompiledShadersPath() const;
  fs::path GetRawShadersPath() const;
  void SetRawShadersPath(const fs::path& path);

  static std::string ReadFileAsString(const fs::path& filePath);
  template<typename T>
  Array<T> ReadFile(const fs::path &filePath);

  static void WriteStringToFile(const fs::path &filePath,const std::string &data);

  static void SelectFolder(std::vector<fs::path> &result,
                           bool bSelectMultiple = false,
                           const std::string &title = "Select Folder");

  static void SelectFiles(std::vector<fs::path> &result,
                          bool bSelectMultiple = false,
                          const std::string &title = "Select File",
                          const std::string &filter = "");
};

template <typename T> Array<T> IoSubsystem::ReadFile(const fs::path &filePath) {
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
