#include "aerox/io/IoSubsystem.hpp"
#include "aerox/platform.hpp"
#ifdef VENGINE_PLATFORM_WIN
#include <shlobj.h>
#include <combaseapi.h>
#endif

namespace aerox::io {
String IoSubsystem::GetName() const {
  return "io";
}

void IoSubsystem::OnInit(Engine *owner) {
  EngineSubsystem::OnInit(owner);
#ifdef VENGINE_PLATFORM_WIN
  CoInitializeEx(NULL,COINIT::COINIT_MULTITHREADED);
#endif
}

void IoSubsystem::OnDestroy() {
  EngineSubsystem::OnDestroy();
}

fs::path IoSubsystem::GetApplicationPath() const {
  return fs::current_path();
}

fs::path IoSubsystem::GetCompiledShadersPath() const {
  return GetApplicationPath() / "shaders";
}

fs::path IoSubsystem::GetRawShadersPath() const {
  return _rawShadersPath;
}

void IoSubsystem::SetRawShadersPath(const fs::path &path) {
  _rawShadersPath = path;
}

std::string IoSubsystem::ReadFileAsString(const fs::path &filePath) {
  std::ifstream fileStream(filePath);
  
  std::string fileData(std::istreambuf_iterator<char>{fileStream},{});

  fileStream.close();
  return fileData;
}

void IoSubsystem::WriteStringToFile(const fs::path &filePath,
    const std::string &data) {
  std::ofstream out(filePath);
  out <<  data;
  out.close();
}

void IoSubsystem::SelectFolder(std::vector<fs::path> &result,
                               bool bSelectMultiple, const std::string &title) {
#ifdef VENGINE_PLATFORM_WIN
  IFileOpenDialog * fd;
  if(SUCCEEDED(CoCreateInstance(CLSID_FileOpenDialog,NULL,CLSCTX_INPROC_SERVER,IID_PPV_ARGS(&fd)))) {
    DWORD options;
    
    if(bSelectMultiple && SUCCEEDED(fd->GetOptions(&options))) {
      fd->SetOptions(options | FOS_ALLOWMULTISELECT | FOS_PICKFOLDERS);
    }

    fd->SetTitle(std::wstring(title.begin(),title.end()).c_str());
    
    if(SUCCEEDED(fd->Show(NULL))) {

      if(bSelectMultiple) {
        IShellItemArray *sia;
        if(SUCCEEDED(fd->GetResults(&sia))) {
          DWORD count = 0;
          if(SUCCEEDED(sia->GetCount(&count))) {
            for(DWORD i = 0; i < count; i++) {
              IShellItem *si;
              if(SUCCEEDED(sia->GetItemAt(i,&si))) {
                PWSTR filePath = nullptr;
        
                if(SUCCEEDED(si->GetDisplayName(SIGDN_DESKTOPABSOLUTEPARSING,&filePath))) {
                  result.emplace_back(filePath);
                  CoTaskMemFree(filePath);
                }
          
                si->Release();
              }
            }
          }
          sia->Release();
        }
      }
      else {
        IShellItem *si;
        if(SUCCEEDED(fd->GetResult(&si))) {
          PWSTR filePath = nullptr;
        
          if(SUCCEEDED(si->GetDisplayName(SIGDN_DESKTOPABSOLUTEPARSING,&filePath))) {
            result.emplace_back(filePath);
            CoTaskMemFree(filePath);
          }
          
          si->Release();
        }
        
      }
      
    }

    fd->Release();
  }
#endif
  
}


void IoSubsystem::SelectFiles(std::vector<fs::path> &result,
                              bool bSelectMultiple, const std::string &title,
                              const std::string &filter) {

#ifdef VENGINE_PLATFORM_WIN
  IFileOpenDialog * fd;
  if(SUCCEEDED(CoCreateInstance(CLSID_FileOpenDialog,NULL,CLSCTX_INPROC_SERVER,IID_PPV_ARGS(&fd)))) {
    DWORD options;
    
    if(bSelectMultiple && SUCCEEDED(fd->GetOptions(&options))) {
      fd->SetOptions(options | FOS_ALLOWMULTISELECT);
    }

    fd->SetTitle(std::wstring(title.begin(),title.end()).c_str());

    if(!filter.empty()) {
      const std::wstring extensions(filter.begin(),filter.end());
    
      COMDLG_FILTERSPEC rgSpec[]  =
      { 
        { L"Filter", extensions.c_str() },
        { L"All", L"*.*" },
      };
      fd->SetFileTypes(ARRAYSIZE(rgSpec),rgSpec);
    } else {
      COMDLG_FILTERSPEC rgSpec[]  =
      { 
        { L"All", L"*.*" },
      };
      fd->SetFileTypes(ARRAYSIZE(rgSpec),rgSpec);
    }
    
    if(SUCCEEDED(fd->Show(NULL))) {

      if(bSelectMultiple) {
        IShellItemArray *sia;
        if(SUCCEEDED(fd->GetResults(&sia))) {
          DWORD count = 0;
          if(SUCCEEDED(sia->GetCount(&count))) {
            for(DWORD i = 0; i < count; i++) {
              IShellItem *si;
              if(SUCCEEDED(sia->GetItemAt(i,&si))) {
                PWSTR filePath = nullptr;
        
                if(SUCCEEDED(si->GetDisplayName(SIGDN_DESKTOPABSOLUTEPARSING,&filePath))) {
                  result.emplace_back(filePath);
                  CoTaskMemFree(filePath);
                }
          
                si->Release();
              }
            }
          }
          sia->Release();
        }
      }
      else {
        IShellItem *si;
        if(SUCCEEDED(fd->GetResult(&si))) {
          PWSTR filePath = nullptr;
        
          if(SUCCEEDED(si->GetDisplayName(SIGDN_DESKTOPABSOLUTEPARSING,&filePath))) {
            result.emplace_back(filePath);
            CoTaskMemFree(filePath);
          }
          
          si->Release();
        }
        
      }
      
    }

    fd->Release();
  }
#endif
  
}
}
