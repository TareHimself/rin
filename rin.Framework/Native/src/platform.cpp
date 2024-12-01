#include "platform.hpp"

#include <string>
#ifdef AEROX_PLATFORM_WIN
#include <shlobj.h>
#include <combaseapi.h>
#include <stringapiset.h>
#endif


EXPORT_IMPL int platformGet()
{
#ifdef AEROX_PLATFORM_WIN
    return static_cast<int>(EPlatform::Windows);
#endif

#ifdef AEROX_PLATFORM_MAC
    return static_cast<int>(EPlatform::Mac);
#endif

#ifdef AEROX_PLATFORM_LINUX
    return static_cast<int>(EPlatform::Linux);
#endif
    
}

EXPORT_IMPL void platformInit()
{
#ifdef AEROX_PLATFORM_WIN
    CoInitializeEx(nullptr,COINIT::COINIT_MULTITHREADED);
#endif  
}

EXPORT_IMPL void platformSelectFile(const char* title, bool multiple, const char* filter, PathReceivedCallback callback)
{
#ifdef AEROX_PLATFORM_WIN
    IFileOpenDialog * fd;
    if(SUCCEEDED(CoCreateInstance(CLSID_FileOpenDialog,NULL,CLSCTX_INPROC_SERVER,IID_PPV_ARGS(&fd)))) {
        DWORD options;
    
        if(multiple && SUCCEEDED(fd->GetOptions(&options))) {
            fd->SetOptions(options | FOS_ALLOWMULTISELECT);
        }
        std::string sTitle(title);
        fd->SetTitle(std::wstring(sTitle.begin(),sTitle.end()).c_str());

        std::string sFilter(filter);
        
        if(!sFilter.empty()) {
            const std::wstring extensions(sFilter.begin(),sFilter.end());
    
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

            if(multiple) {
                IShellItemArray *sia;
                if(SUCCEEDED(fd->GetResults(&sia))) {
                    DWORD count = 0;
                    if(SUCCEEDED(sia->GetCount(&count))) {
                        for(DWORD i = 0; i < count; i++) {
                            IShellItem *si;
                            if(SUCCEEDED(sia->GetItemAt(i,&si))) {
                                PWSTR filePath = nullptr;
        
                                if(SUCCEEDED(si->GetDisplayName(SIGDN_DESKTOPABSOLUTEPARSING,&filePath))) {
                                    std::wstring wstr(filePath);
                                    const int sizeNeeded = WideCharToMultiByte(CP_UTF8, 0, wstr.data(), static_cast<int>(wstr.size()), nullptr, 0, nullptr, nullptr);
                                    std::string strTo( sizeNeeded, 0 );
                                    WideCharToMultiByte                  (CP_UTF8, 0, wstr.data(), static_cast<int>(wstr.size()),
                                                                          strTo.data(), sizeNeeded, nullptr, nullptr);
                                    callback(strTo.c_str());
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
                        const std::wstring wstr(filePath);
                        const int sizeNeeded = WideCharToMultiByte(CP_UTF8, 0, wstr.data(), static_cast<int>(wstr.size()), nullptr, 0, nullptr, nullptr);
                        std::string strTo( sizeNeeded, 0 );
                        WideCharToMultiByte                  (CP_UTF8, 0, wstr.data(), static_cast<int>(wstr.size()),
                                                              strTo.data(), sizeNeeded, nullptr, nullptr);
                        callback(strTo.c_str());
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

EXPORT_IMPL void platformSelectPath(const char* title, bool multiple, PathReceivedCallback callback)
{
#ifdef AEROX_PLATFORM_WIN
    IFileOpenDialog * fd;
    if(SUCCEEDED(CoCreateInstance(CLSID_FileOpenDialog,NULL,CLSCTX_INPROC_SERVER,IID_PPV_ARGS(&fd)))) {
        DWORD options;
    
        if(multiple && SUCCEEDED(fd->GetOptions(&options))) {
            fd->SetOptions(options | FOS_ALLOWMULTISELECT | FOS_PICKFOLDERS);
        }
        else if(SUCCEEDED(fd->GetOptions(&options)))
        {
            fd->SetOptions(options | FOS_PICKFOLDERS);
        }
        
        std::string sTitle(title);
        fd->SetTitle(std::wstring(sTitle.begin(),sTitle.end()).c_str());
    
        if(SUCCEEDED(fd->Show(NULL))) {

            if(multiple) {
                IShellItemArray *sia;
                if(SUCCEEDED(fd->GetResults(&sia))) {
                    DWORD count = 0;
                    if(SUCCEEDED(sia->GetCount(&count))) {
                        for(DWORD i = 0; i < count; i++) {
                            IShellItem *si;
                            if(SUCCEEDED(sia->GetItemAt(i,&si))) {
                                PWSTR filePath = nullptr;
        
                                if(SUCCEEDED(si->GetDisplayName(SIGDN_DESKTOPABSOLUTEPARSING,&filePath))) {
                                    const std::wstring wstr(filePath);
                                    const int sizeNeeded = WideCharToMultiByte(CP_UTF8, 0, wstr.data(), static_cast<int>(wstr.size()), nullptr, 0, nullptr, nullptr);
                                    std::string strTo( sizeNeeded, 0 );
                                    WideCharToMultiByte                  (CP_UTF8, 0, wstr.data(), static_cast<int>(wstr.size()),
                                                                          strTo.data(), sizeNeeded, nullptr, nullptr);
                                    callback(strTo.c_str());
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
                        const std::wstring wstr(filePath);
                        const int sizeNeeded = WideCharToMultiByte(CP_UTF8, 0, wstr.data(), static_cast<int>(wstr.size()), nullptr, 0, nullptr, nullptr);
                        std::string strTo( sizeNeeded, 0 );
                        WideCharToMultiByte                  (CP_UTF8, 0, wstr.data(), static_cast<int>(wstr.size()),
                                                              strTo.data(), sizeNeeded, nullptr, nullptr);
                        callback(strTo.c_str());
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
